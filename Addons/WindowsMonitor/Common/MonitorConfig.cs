using YamlDotNet.Serialization;

namespace IOTLinkAddon.Common
{
    public class MonitorConfig
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "interval")]
        public int Interval { get; set; }
    }
}
