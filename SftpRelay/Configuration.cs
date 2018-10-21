namespace SftpRelay
{
    using System.IO;
    using Newtonsoft.Json;

    internal class Configuration
    {
        public SourceService Source { get; set; }
        public DestinationService Destination { get; set; }
        public string LogTo { get; set; }

        public static Configuration Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Configuration>(json);
        }
    }
}
