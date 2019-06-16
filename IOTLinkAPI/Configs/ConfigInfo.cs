using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using System.IO;
using static IOTLinkAPI.Helpers.ConfigHelper;

namespace IOTLinkAPI.Configs
{
    internal class ConfigInfo
    {
        public object Config { get; set; }

        public FileSystemWatcher FileSystemWatcher { get; set; }

        public ConfigType ConfigType { get; set; }

        public event ConfigReloadedHandler OnConfigReloadHandler;

        public void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            OnConfigReloadHandler?.Invoke(sender, e);
        }
    }
}
