namespace SftpRelay
{
    using System;

    public class SftpFile : IEquatable<SftpFile>
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public DateTimeOffset LastModified { get; set; }

        public bool Equals(SftpFile other)
        {
            if (ReferenceEquals(this, other)) return true;
            return StringComparer.OrdinalIgnoreCase.Equals(FileName, other.FileName)
                && StringComparer.OrdinalIgnoreCase.Equals(Path, other.Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SftpFile)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(FileName);
        }
    }
}