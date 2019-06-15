using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace IOTLinkAddon.Common
{
    public class MonitorConfig
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "monitors")]
        public Dictionary<string, int> Monitors { get; set; }
    }
}
