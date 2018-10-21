namespace SftpRelay
{
    using System.IO;
    using System.Linq;
    using System.Text;

    internal class DuplicatingTextWriter : TextWriter
    {
        private readonly TextWriter[] underlyingWriters;

        public DuplicatingTextWriter(params TextWriter[] underlyingWriters)
        {
            this.underlyingWriters = underlyingWriters;
        }

        public override Encoding Encoding => underlyingWriters.First().Encoding;

        public override void Close()
        {
            foreach (var writer in underlyingWriters)
                writer.Close();
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var writer in underlyingWriters)
                writer.Dispose();
        }

        public override void Flush()
        {
            foreach (var writer in underlyingWriters)
                writer.Flush();
        }

        public override void Write(string value)
        {
            foreach (var writer in underlyingWriters)
                writer.Write(value);
        }

        public override void Write(object value)
        {
            foreach (var writer in underlyingWriters)
                writer.Write(value);
        }

        public override void WriteLine(string value)
        {
            foreach (var writer in underlyingWriters)
                writer.WriteLine(value);
        }

        public override void WriteLine(object value)
        {
            foreach (var writer in underlyingWriters)
                writer.WriteLine(value);
        }
    }
}
