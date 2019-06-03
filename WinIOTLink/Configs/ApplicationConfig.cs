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

        public class MqttConfig
        {
            [YamlMember(Alias = "username")]
            public String Username { get; set; }

            [YamlMember(Alias = "password")]
            public String Password { get; set; }

            [YamlMember(Alias = "hostname")]
            public String Hostname { get; set; }

            [YamlMember(Alias = "webSocketUrl")]
            public String WebSocketUrl { get; set; }

            [YamlMember(Alias = "clientId")]
            public String ClientId { get; set; }

            [YamlMember(Alias = "prefix")]
            public String Prefix { get; set; }

            [YamlMember(Alias = "tlsEnabled")]
            public Boolean TLSEnabled { get; set; }

            [YamlMember(Alias = "webSocket")]
            public Boolean WebSocket { get; set; }

            [YamlMember(Alias = "cleanSession")]
            public Boolean CleanSession { get; set; }

            [YamlMember(Alias = "port")]
            public int Port { get; set; }
        }
    }
}
