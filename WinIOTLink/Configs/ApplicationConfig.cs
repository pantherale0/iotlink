using System;
using YamlDotNet.Serialization;

namespace WinIOTLink.Configs
{
    public class ApplicationConfig
    {
        [YamlMember(Alias = "monitor")]
        public MonitorConfig Monitor { get; set; }

        [YamlMember(Alias = "mqtt")]
        public MqttConfig MQTT { get; set; }

        public class MonitorConfig
        {
            [YamlMember(Alias = "interval")]
            public int Interval { get; set; }
        }
    }
}
