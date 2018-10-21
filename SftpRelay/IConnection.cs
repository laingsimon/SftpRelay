namespace SftpRelay
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    internal interface IConnection: IDisposable
    {
        Task Open();
        void Close();
        Task ChangeDirectory(string newDirectory);
        Task<IEnumerable<SftpFile>> GetFiles(string relativePath = null);
        Task<IEnumerable<SftpDirectory>> GetDirectories(string relativePath = null);
        Task<Stream> GetContent(SftpFile file);
        Task CreateFile(SftpFile file, Stream fileContent);
        Task<bool> DirectoryExists(string relativePath);
        Task CreateDirectory(string relativePath);
        Task DeleteFile(SftpFile file);
    }
}
