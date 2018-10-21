namespace SftpRelay
{
    using System.Threading.Tasks;

    internal class Service
    {
        public string HostName { get; set; }
        public int Port { get; set; } = 22;
        public string DirectoryPath { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool VerboseLogging { get; set; } = false;

        public async Task<IConnection> Connect()
        {
            var connection = CreateConnection();
            await connection.Open();

            if (!string.IsNullOrEmpty(DirectoryPath))
                await connection.ChangeDirectory(DirectoryPath);

            return connection;
        }

        private IConnection CreateConnection()
        {
            if (string.IsNullOrEmpty(HostName))
                return new LocalDiskConnection(this);

            return new SftpConnection(this, VerboseLogging);
        }
    }
}