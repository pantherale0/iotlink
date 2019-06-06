using IOTLink.Helpers;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace IOTLink.Configs
{
    public class ApplicationConfig
    {
        [YamlMember(Alias = "monitor")]
        public MonitorConfig Monitor { get; set; }

        [YamlMember(Alias = "mqtt")]
        public MqttConfig MQTT { get; set; }

        [YamlMember(Alias = "addons")]
        public AddonsConfiguration Addons { get; set; }

        [YamlMember(Alias = "logging")]
        public LoggingConfiguration Logging { get; set; }

        public class MonitorConfig
        {
            [YamlMember(Alias = "enabled")]
            public bool Enabled { get; set; }

            [YamlMember(Alias = "interval")]
            public int Interval { get; set; }
        }

        public class AddonsConfiguration
        {
            [YamlMember(Alias = "enabled")]
            public bool Enabled { get; set; }

            [YamlMember(Alias = "globalTopics")]
            public List<string> GlobalTopics { get; set; }
        }

        public class LoggingConfiguration
        {
            [YamlMember(Alias = "enabled")]
            public bool Enabled { get; set; }

            [YamlMember(Alias = "level")]
            public LoggerHelper.LogLevel Level { get; set; }
        }
    }
}
