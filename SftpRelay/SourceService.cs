namespace SftpRelay
{
    internal class SourceService : Service
    {
        public string FileNameFilter { get; set; } = "*.*";
        public bool RecurseSubDirectories { get; set; } = true;
        public string[] ExcludeDirectories { get; set; }
        public string[] ExcludeFiles { get; set; }
        public bool MoveFiles { get; set; }
    }
}