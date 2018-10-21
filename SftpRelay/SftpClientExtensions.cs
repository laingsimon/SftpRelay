namespace SftpRelay
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Renci.SshNet;
    using Renci.SshNet.Sftp;

    internal static class SftpClientExtensions
    {
        public static async Task ConnectAsync(this SftpClient client)
        {
            await Task.Factory.StartNew(client.Connect);
        }

        public static async Task ChangeDirectoryAsync(this SftpClient client, string path)
        {
            await Task.Factory.StartNew(() => client.ChangeDirectory(path));
        }

        public static async Task<SftpFileStream> OpenReadAsync(this SftpClient client, string path)
        {
            return await Task.Factory.StartNew(() => client.OpenRead(path));
        }

        public static async Task UploadFileAsync(this SftpClient client, Stream input, string path, Action<ulong> uploadCallback = null)
        {
            await Task.Factory.StartNew(() => client.UploadFile(input, path, uploadCallback));
        }

        public static async Task<SftpFileAttributes> GetAttributesAsync(this SftpClient client, string path)
        {
            return await Task.Factory.StartNew(() => client.GetAttributes(path));
        }

        public static async Task SetAttributesAsync(this SftpClient client, string path, SftpFileAttributes attributes)
        {
            await Task.Factory.StartNew(() => client.SetAttributes(path, attributes));
        }

        public static async Task DeleteFileAsync(this SftpClient client, string path)
        {
            await Task.Factory.StartNew(() => client.DeleteFile(path));
        }
    }
}
