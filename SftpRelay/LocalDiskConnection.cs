namespace SftpRelay
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    internal class LocalDiskConnection : IConnection
    {
        private readonly bool setAttributes;
        private readonly string[] excludeFiles;
        private DirectoryInfo currentDirectory;

        public LocalDiskConnection(Service service)
        {
            this.currentDirectory = new DirectoryInfo(service.DirectoryPath);
            this.setAttributes = (service as DestinationService)?.SetAttributes == true;
            this.excludeFiles = (service as SourceService)?.ExcludeFiles;
        }

        public void Dispose()
        { }

        public Task Open()
        {
            return Task.FromResult<object>(null);
        }

        public void Close()
        { }

        public Task ChangeDirectory(string newDirectory)
        {
            currentDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, newDirectory));
            return Task.FromResult<object>(null);
        }

        public Task<IEnumerable<SftpFile>> GetFiles(string relativePath = null)
        {
            var directory = string.IsNullOrEmpty(relativePath)
                ? currentDirectory
                : new DirectoryInfo(Path.Combine(currentDirectory.FullName, relativePath));

            return Task.FromResult(from file in directory.EnumerateFiles()
                                   where ShouldIncludeFile(file)
                select new SftpFile
                {
                    FileName = file.Name,
                    LastModified = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero),
                    Size = file.Length,
                    Path = relativePath
                });
        }

        private bool ShouldIncludeFile(FileInfo file)
        {
            if (this.excludeFiles == null)
                return true;

            if (this.excludeFiles.Any(f => f.Equals(file.Name, StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

        public Task<IEnumerable<SftpDirectory>> GetDirectories(string relativePath = null)
        {
            var directory = string.IsNullOrEmpty(relativePath)
                ? currentDirectory
                : new DirectoryInfo(Path.Combine(currentDirectory.FullName, relativePath));

            return Task.FromResult(from file in directory.EnumerateDirectories()
                select new SftpDirectory
                {
                    Name = file.Name
                });
        }

        public Task<bool> DirectoryExists(string relativePath)
        {
            var directory = string.IsNullOrEmpty(relativePath)
                ? currentDirectory
                : new DirectoryInfo(Path.Combine(currentDirectory.FullName, relativePath));

            return Task.FromResult(directory.Exists);
        }

        public Task CreateDirectory(string relativePath)
        {
            return Task.Factory.StartNew(() =>
            {
                var directory = string.IsNullOrEmpty(relativePath)
                    ? currentDirectory
                    : new DirectoryInfo(Path.Combine(currentDirectory.FullName, relativePath));

                if (!directory.Exists)
                    directory.Create();
            });
        }

        public Task DeleteFile(SftpFile file)
        {
            var path = string.IsNullOrEmpty(file.Path)
                ? Path.Combine(currentDirectory.FullName, file.FileName)
                : Path.Combine(currentDirectory.FullName, file.Path, file.FileName);

            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<Stream> GetContent(SftpFile file)
        {
            var path = string.IsNullOrEmpty(file.Path)
                ? Path.Combine(currentDirectory.FullName, file.FileName)
                : Path.Combine(currentDirectory.FullName, file.Path, file.FileName);
            return Task.FromResult<Stream>(File.OpenRead(path));
        }

        public async Task CreateFile(SftpFile file, Stream fileContent)
        {
            var path = string.IsNullOrEmpty(file.Path)
                ? Path.Combine(currentDirectory.FullName, file.FileName)
                : Path.Combine(currentDirectory.FullName, file.Path, file.FileName);

            using (var writeStream = File.OpenWrite(path))
                await fileContent.CopyToAsync(writeStream);

            if (this.setAttributes)
                File.SetLastWriteTimeUtc(path, file.LastModified.UtcDateTime);
        }
    }
}
