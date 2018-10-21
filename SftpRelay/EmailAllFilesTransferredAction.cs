namespace SftpRelay
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using RazorEngine;
    using RazorEngine.Configuration;
    using RazorEngine.Templating;

    internal class EmailAllFilesTransferredAction : IRelayFileAction
    {
        private readonly List<SftpFileComparison> relayed = new List<SftpFileComparison>();
        public void FileRelayed(SftpFileComparison file)
        {
            relayed.Add(file);
        }

        public async Task Finished(string path, string[] args)
        {
            if (!relayed.Any())
                return;

            var settings = ReadSettings(path, args.First());
            var html = GetEmailBody(path, settings);

            var message = new MailMessage(settings.From, settings.To, settings.Subject, html)
            {
                IsBodyHtml = true
            };

            using (var smtpServer = new SmtpClient(settings.Server, settings.Port))
            {
                smtpServer.EnableSsl = true;
                smtpServer.Credentials = new NetworkCredential()
                {
                    UserName = settings.Username,
                    Password = settings.Password
                };

                await smtpServer.SendMailAsync(message);
            }
        }

        private EmailSettings ReadSettings(string path, string configFilename)
        {
            var configFilenameWithoutDev = Regex.Replace(configFilename, @"\.dev", "", RegexOptions.IgnoreCase);
            var json = File.ReadAllText(Path.Combine(path, Path.ChangeExtension(configFilenameWithoutDev, ".email.json")));
            return JsonConvert.DeserializeObject<EmailSettings>(json);
        }

        private string GetEmailBody(string path, EmailSettings settings)
        {
            var template = File.ReadAllText(Path.Combine(path, settings.BodyTemplate));

            var config = new TemplateServiceConfiguration
            {
                DisableTempFileLocking = true,
                CachingProvider = new DefaultCachingProvider(t => { })
            };
            //disables the warnings
            Engine.Razor = RazorEngineService.Create(config); // new API

            return Engine.Razor.RunCompile(template, "templateKey", relayed.GetType(), relayed);
        }
    }
}
