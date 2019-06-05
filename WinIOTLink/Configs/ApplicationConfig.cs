using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace WinIOTLink.Configs
{
    public class ApplicationConfig
    {
        [YamlMember(Alias = "monitor")]
        public MonitorConfig Monitor { get; set; }

        [YamlMember(Alias = "mqtt")]
        public MqttConfig MQTT { get; set; }

        [YamlMember(Alias = "addons")]
        public AddonsConfiguration Addons { get; set; }

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
    }
}
