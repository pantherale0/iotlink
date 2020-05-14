using IOTLinkAPI.Configs;

namespace IOTLinkAddon.Common.Configs
{
    public class MonitoringConfig
    {
        public bool Enabled { get; set; }
        public int CompareType { get; set; }
        public bool Discoverable { get; set; }
        public bool Cacheable { get; set; }
        public bool Grouped { get; set; }
        public int Interval { get; set; }

        public static MonitoringConfig FromConfiguration(Configuration configuration)
        {
            return new MonitoringConfig
            {
                Enabled = configuration.GetValue("enabled", false),
                CompareType = configuration.GetValue("compareType", 0),
                Discoverable = configuration.GetValue("enabled", false),
                Cacheable = configuration.GetValue("cacheable", false),
                Grouped = configuration.GetValue("grouped", false),
                Interval = configuration.GetValue("interval", 0)
            };
        }
    }
}
