namespace SftpRelay
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Threading.Tasks;

    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Execute(args).Wait();
            }
            finally
            {
                if (Debugger.IsAttached)
                    Console.ReadKey();
            }
        }

        private static async Task Execute(string[] args)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ConsoleTraceListener());
            var path = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var logPath = $"{path}\\{DateTime.Now:yyyy-MM-dd}";
            var deleteOldLogs = !Directory.Exists(logPath);
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            var logListener = new LogFileTraceListener($"{logPath}\\{DateTime.Now:yyyyMMdd-HHmmss}-{Path.GetFileNameWithoutExtension(args.First())}.log");
            Trace.Listeners.Add(logListener);

            if (deleteOldLogs)
                RemoveOldLogDirectories(path);

            try
            {
                var configuration = Configuration.Load(Path.Combine(path, args.First()));
                var relay = new Relay(configuration.Source, configuration.Destination);

                await relay.RelayFiles();

                await (configuration.Destination?.RelayFileAction?.Finished(path, args) ?? Task.CompletedTask);
            }
            catch (Exception exc)
            {
                Trace.TraceError(exc.ToString());
            }
            finally
            {
                logListener.Flush();

                if (logListener.Errors.Any())
                    EmailError(logListener);

                if (Debugger.IsAttached)
                    Console.ReadKey();
            }
        }

        private static void EmailError(LogFileTraceListener listener)
        {
            using (var client = new SmtpClient(Properties.Settings.Default.SmtpServer))
            {
                var toAddresses = Properties.Settings.Default.EmailRecipients
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(addr => new MailAddress(addr))
                    .ToArray();

                var message = new MailMessage
                {
                    From = new MailAddress(Properties.Settings.Default.SmtpFromAddress),
                    Subject = Properties.Settings.Default.EmailSubject,
                    Body = $@"
<body>
{string.Join("\r\n", listener.Errors.Select(e => $"<h3>{e}</h3>"))}
<hr />
<pre>
{listener.GetFileContent()}
</pre>
<hr />
{DateTime.Now:dd/MM/yyyy HH:mm:ss} on {Environment.MachineName} @ {Environment.CurrentDirectory}
</body>",
                    IsBodyHtml = true
                };

                foreach (var toAddress in toAddresses)
                    message.To.Add(toAddress);

                client.Send(message);
            }
        }

        private static void RemoveOldLogDirectories(string path, int keepDays = 5)
        {
            Trace.TraceInformation($"Finding old log directories; keeping {keepDays} within {path}");
            var logDirectories = from directoryPath in Directory.EnumerateDirectories(path)
                let directoryDate = GetDirectoryNameAsDate(Path.GetFileName(directoryPath))
                where directoryDate != null
                orderby directoryDate.Value descending
                select directoryPath;

            var directoriesToDelete = logDirectories.Skip(keepDays);
            foreach (var directory in directoriesToDelete)
            {
                Trace.TraceInformation($"Deleting old log directory: {directory}");
                Directory.Delete(directory, true);
            }
        }

        private static DateTime? GetDirectoryNameAsDate(string path)
        {
            return DateTime.TryParseExact(path, "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime date)
                ? date
                : default(DateTime?);
        }
    }
}
