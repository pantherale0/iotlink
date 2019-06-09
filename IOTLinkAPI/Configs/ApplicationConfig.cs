using IOTLink.Helpers;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace IOTLink.Configs
{
#pragma warning disable 1591
    public class ApplicationConfig
    {
        [YamlMember(Alias = "mqtt")]
        public MqttConfig MQTT { get; set; }

        [YamlMember(Alias = "addons")]
        public AddonsConfiguration Addons { get; set; }

        [YamlMember(Alias = "logging")]
        public LoggingConfiguration Logging { get; set; }

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
