namespace SftpRelay
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Renci.SshNet;

    internal class SftpConnection : IConnection
    {
        private readonly Service service;
        private readonly bool verboseLogging;
        private readonly bool setAttributes;
        private readonly string[] excludeFiles;
        private string directory = "/";
        private SftpClient client;

        public SftpConnection(Service service, bool verboseLogging)
        {
            this.service = service;
            this.setAttributes = (service as DestinationService)?.SetAttributes == true;
            this.excludeFiles = (service as SourceService)?.ExcludeFiles;
            this.verboseLogging = verboseLogging;
        }

        public async Task Open()
        {
            if (this.client != null)
            {
                if (this.client.IsConnected)
                    return;

                this.client.Connect();
                return;
            }

            if (verboseLogging)
                Trace.TraceInformation($"Opening connection to: {service.HostName}:{service.Port}");

            var info = new ConnectionInfo(
                service.HostName,
                service.Port,
                service.UserName,
                new PasswordAuthenticationMethod(service.UserName, service.Password));
            this.client = new SftpClient(info);
            await this.client.ConnectAsync();

            Trace.TraceInformation($"Connected to: {service.HostName}:{service.Port}");
        }

        public void Close()
        {
            if (verboseLogging)
                Trace.TraceInformation($"Disconnecting from {service.HostName}:{service.Port}");
            client?.Disconnect();

            Trace.TraceInformation($"Disconnected from {service.HostName}:{service.Port}");
        }

        public void Dispose()
        {
            if (this.client == null)
                return;

            Close();
            this.client.Dispose();
            this.client = null;
        }

        public async Task ChangeDirectory(string newDirectory)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            if (verboseLogging)
                Trace.TraceInformation($"Changing directory on {service.HostName}:{service.Port} to {newDirectory}");

            await this.client.ChangeDirectoryAsync(newDirectory);
            this.directory = "/" + newDirectory;
        }

        public async Task<IEnumerable<SftpFile>> GetFiles(string relativePath = null)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var path = string.IsNullOrEmpty(relativePath)
                ? $"{directory}"
                : $"{directory}/{relativePath}";
            Trace.TraceInformation($"Getting files from {path} on {service.HostName}:{service.Port}...");

            var beginResult = this.client.BeginListDirectory(path, _ => { }, null, i => { });
            var result = await Task.Factory.FromAsync(beginResult, this.client.EndListDirectory, TaskCreationOptions.None);

            return from item in result
                   where !item.IsDirectory
                   where ShouldIncludeFile(item)
                   select new SftpFile
                   {
                       FileName = item.Name,
                       LastModified = new DateTimeOffset(item.LastWriteTimeUtc, TimeSpan.Zero),
                       Size = item.Length,
                       Path = relativePath
                   };
        }

        private bool ShouldIncludeFile(Renci.SshNet.Sftp.SftpFile file)
        {
            if (this.excludeFiles == null)
                return true;

            if (this.excludeFiles.Any(f => f.Equals(file.Name, StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

        public async Task<IEnumerable<SftpDirectory>> GetDirectories(string relativePath = null)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var path = string.IsNullOrEmpty(relativePath)
                ? $"{directory}"
                : $"{directory}/{relativePath}";
            Trace.TraceInformation($"Getting directories from {path} on {service.HostName}:{service.Port}...");

            var beginResult = this.client.BeginListDirectory(path, _ => { }, null, i => { });
            var result = await Task.Factory.FromAsync(beginResult, this.client.EndListDirectory, TaskCreationOptions.None);

            return from item in result
                where item.IsDirectory && item.Name != "." && item.Name != ".."
                select new SftpDirectory
                {
                    Name = item.Name
                };
        }

        public async Task<bool> DirectoryExists(string relativePath)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var path = string.IsNullOrEmpty(relativePath)
                ? $"{directory}"
                : $"{directory}/{relativePath}";
            var directoryName = Path.GetFileName(path);
            path = path.Substring(0, path.Length - (directoryName.Length + 1));

            var beginResult = this.client.BeginListDirectory(path, _ => { }, null, i => { });
            var result = await Task.Factory.FromAsync(beginResult, this.client.EndListDirectory, TaskCreationOptions.None);

            return result.Any(f => f.IsDirectory && f.Name.Equals(directoryName, StringComparison.OrdinalIgnoreCase));
        }

        public Task CreateDirectory(string relativePath)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var path = string.IsNullOrEmpty(relativePath)
                ? directory
                : $"{directory}/{relativePath}";

            Trace.TraceInformation($"Creating directory at path {path} on {service.HostName}:{service.Port}...");

            return Task.Factory.StartNew(() => this.client.CreateDirectory(path));
        }

        public async Task DeleteFile(SftpFile file)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var subPath = string.IsNullOrEmpty(file.Path)
                ? file.FileName
                : $"{file.Path}/{file.FileName}";
            Trace.TraceInformation($"Deleting file {directory}/{subPath} from {service.HostName}:{service.Port}");

            await this.client.DeleteFileAsync($"{directory}/{subPath}");
        }

        public async Task<Stream> GetContent(SftpFile file)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var subPath = string.IsNullOrEmpty(file.Path)
                ? file.FileName
                : $"{file.Path}/{file.FileName}";
            Trace.TraceInformation($"Getting content of {directory}/{subPath} ({file.Size / 1024.0:n1}kb) from {service.HostName}:{service.Port}");

            return await this.client.OpenReadAsync($"{directory}/{subPath}");
        }

        public async Task CreateFile(SftpFile file, Stream fileContent)
        {
            if (this.client == null)
                throw new InvalidOperationException($"Not connected to {service.HostName}:{service.Port}");

            var subPath = string.IsNullOrEmpty(file.Path)
                ? $"{directory}/{file.FileName}"
                : $"{directory}/{file.Path}/{file.FileName}";
            Trace.TraceInformation($"Creating file {subPath} on {service.HostName}:{service.Port}");

            await this.client.UploadFileAsync(fileContent, subPath);
            var fileAttributes = await this.client.GetAttributesAsync(subPath);
            fileAttributes.LastWriteTime = file.LastModified.LocalDateTime;

            if (this.setAttributes)
                await this.client.SetAttributesAsync(subPath, fileAttributes);

            if (verboseLogging)
                Trace.TraceInformation($"Created file {subPath} on {service.HostName}:{service.Port}");
        }
    }
}