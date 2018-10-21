namespace SftpRelay
{
    internal class EmailSettings
    {
        public EmailSettings()
        {
            this.Subject = "SFTP files relayed";
            this.Server = "localhost";
            this.BodyTemplate = "email.cshtml";
        }

        public string Subject { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string Server { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string BodyTemplate { get; set; }
    }
}