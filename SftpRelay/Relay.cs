namespace SftpRelay
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    internal class Relay
    {
        private readonly SourceService source;
        private readonly DestinationService destination;

        public Relay(SourceService source, DestinationService destination)
        {
            this.source = source;
            this.destination = destination;
        }

        public async Task RelayFiles()
        {
            using (var sourceConnection = await source.Connect())
            using (var destinationConnection = await destination.Connect())
            {
                await RelayDirectories(sourceConnection, destinationConnection);
            }
        }

        private async Task<int> RelayDirectories(IConnection sourceConnection, IConnection destinationConnection, string relativePath = null)
        {
            var relayed = 0;
            if (source.RecurseSubDirectories)
            {
                var sourceDirectories = (await sourceConnection.GetDirectories(relativePath))
                    .Where(d => !IsExcluded(d))
                    .ToArray();

                foreach (var directory in sourceDirectories)
                {
                    var subPath = string.IsNullOrEmpty(relativePath)
                        ? directory.Name
                        : $"{relativePath}/{directory.Name}";
                    relayed += await RelayDirectories(sourceConnection, destinationConnection, subPath);
                }

                if (!await destinationConnection.DirectoryExists(relativePath))
                    await destinationConnection.CreateDirectory(relativePath);
            }

            return relayed + await RelayFiles(sourceConnection, destinationConnection, relativePath);
        }

        private bool IsExcluded(SftpDirectory directory)
        {
            if (directory.Name == "." || directory.Name == "..")
                return true;

            if (source.ExcludeDirectories == null || !source.ExcludeDirectories.Any())
                return false;

            return source.ExcludeDirectories.Any(d => d.Equals(directory.Name, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<int> RelayFiles(IConnection sourceConnection, IConnection destinationConnection, string relativePath)
        {
            var relayed = 0;
            var sourceFiles = (await sourceConnection.GetFiles(relativePath)).ToArray();
            var destinationFiles = await destinationConnection.GetFiles(relativePath);

            var sourceFileCount = sourceFiles.Length;
            var matchingSourceFiles = sourceFiles.Where(f => MatchesFileFilter(f.FileName, source.FileNameFilter)).ToArray();
            var matchingSourceFileCount = matchingSourceFiles.Length;

            var filesToRelay = from sourceFile in matchingSourceFiles
                join destinationFile in destinationFiles on sourceFile equals destinationFile
                into outerJoin
                where !outerJoin.Any() ||
                      FileHasChanged(sourceFile, outerJoin.Single()) //where we haven't relayed the file already
                select new SftpFileComparison(sourceFile, outerJoin.SingleOrDefault());

            foreach (var fileToRelay in filesToRelay)
            {
                await RelayFile(fileToRelay, sourceConnection, destinationConnection);
                relayed++;
            }

            Trace.WriteLine($"Relayed {relayed} of {sourceFileCount} file/s of which {matchingSourceFileCount} matched the given filter `{source.FileNameFilter}` in `{relativePath}`");

            return relayed;
        }

        private bool FileHasChanged(SftpFile sourceFile, SftpFile destinationFile)
        {
            if (destination.SetAttributes)
            {
                return destinationFile.Size != sourceFile.Size
                       || sourceFile.LastModified != destinationFile.LastModified;
            }

            return destinationFile.Size != sourceFile.Size
                || sourceFile.LastModified > destinationFile.LastModified;
        }

        private bool MatchesFileFilter(string fullFileName, string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "*.*")
                return true;

            var mask = new Regex(filter.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."));
            return mask.IsMatch(fullFileName);
        }

        private async Task RelayFile(SftpFileComparison fileToRelay, IConnection sourceConnection, IConnection destinationConnection)
        {
            using (var fileContent = await sourceConnection.GetContent(fileToRelay.Source))
            {
                await destinationConnection.CreateFile(fileToRelay.Source, fileContent);
            }

            if (source.MoveFiles)
                await sourceConnection.DeleteFile(fileToRelay.Source);

            destination.RelayFileAction?.FileRelayed(fileToRelay);
        }
    }
}
