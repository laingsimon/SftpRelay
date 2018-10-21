namespace SftpRelay
{
    using System;

    public class SftpFileComparison
    {
        public SftpFile Source { get; }
        public SftpFile Destination { get; }

        public SftpFileComparison(SftpFile source, SftpFile destination = null)
        {
            Source = source;
            Destination = destination;
        }

        public string FileName => Source.FileName;
        public string Path => Source.Path;
        public DateTimeOffset LastModified => Source.LastModified;

        public string KbIncrease
        {
            get
            {
                if (Destination == null)
                    return "";

                var increase = (Destination.Size - Source.Size) / 1024.0d;

                if (increase < 0)
                    return $"{increase:n1}kb";

                return $"+{increase:n1}kb";
            }
        }
    }
}