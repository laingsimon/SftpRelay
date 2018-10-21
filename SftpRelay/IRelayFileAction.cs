namespace SftpRelay
{
    using System.Threading.Tasks;

    internal interface IRelayFileAction
    {
        void FileRelayed(SftpFileComparison file);
        Task Finished(string path, string[] args);
    }
}
