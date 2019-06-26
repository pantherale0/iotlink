using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace IOTLinkAddon.Common
{
    public class WindowsMonitorConfig
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "sizeFormat")]
        public string SizeFormat { get; set; } = "GB";

        [YamlMember(Alias = "monitors")]
        public Dictionary<string, MonitorConfig> Monitors { get; set; }
    }
}
