using System;
using System.Collections.Generic;
using System.Text;

namespace SftpRelay
{
    using System.Diagnostics;
    using System.IO;

    internal class LogFileTraceListener : TraceListener
    {
        private readonly StreamWriter writer;
        private readonly StringBuilder logToDate = new StringBuilder();
        public readonly List<string> Errors = new List<string>();

        public LogFileTraceListener(string fileName)
        {
            this.writer = new StreamWriter(fileName)
            {
                AutoFlush = true
            };
        }

        private void AppendLine(string message, string messageType)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {messageType.Substring(0, 4).ToUpper()} - {message}";
            this.writer.WriteLine(line);
            logToDate.AppendLine(line);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (eventType == TraceEventType.Critical || eventType == TraceEventType.Error)
                Errors.Add(message);

            AppendLine(message, eventType.ToString());
        }

        public override void WriteLine(string message)
        {
            AppendLine(message, "NONE");
        }

        public override void Write(string message)
        {
            AppendLine(message, "NONE");
        }

        public override void Flush()
        {
            writer.Flush();
        }

        public override void Close()
        {
            writer.Close();
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
            Close();
        }

        public string GetFileContent()
        {
            return this.logToDate.ToString();
        }
    }
}
