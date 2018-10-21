namespace SftpRelay
{
    using System;
    using Newtonsoft.Json;

    internal class DestinationService : Service
    {
        private IRelayFileAction fileRelayAction;

        [JsonIgnore]
        public IRelayFileAction RelayFileAction
        {
            get
            {
                if (fileRelayAction != null)
                    return fileRelayAction;

                if (string.IsNullOrEmpty(RelayFileActionTypeName))
                    return null;

                var type = Type.GetType(RelayFileActionTypeName);
                if (type == null)
                    throw new InvalidOperationException($"Cannot find type with name `{RelayFileActionTypeName}`");

                return fileRelayAction = (IRelayFileAction)Activator.CreateInstance(type);
            }
            set { fileRelayAction = value; }
        }

        [JsonIgnore]
        public bool HasRelayFileActionBeenCreated => fileRelayAction != null;

        [JsonProperty("RelayFileAction")]
        public string RelayFileActionTypeName { get; set; }

        public bool SetAttributes { get; set; }
    }
}