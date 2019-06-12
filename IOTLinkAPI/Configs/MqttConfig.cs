using YamlDotNet.Serialization;

namespace IOTLinkAPI.Configs
{
#pragma warning disable 1591
    public class MqttConfig
    {
        [YamlMember(Alias = "credentials")]
        public CredentialConfig Credentials { get; set; }

        [YamlMember(Alias = "tcp")]
        public TCPConfig TCP { get; set; }

        [YamlMember(Alias = "websocket")]
        public WebSocketConfig WebSocket { get; set; }

        [YamlMember(Alias = "messages")]
        public MsgConfig Messages { get; set; }

        [YamlMember(Alias = "lwt")]
        public LWTConfig LWT { get; set; }

        [YamlMember(Alias = "clientId")]
        public string ClientId { get; set; }

        [YamlMember(Alias = "prefix")]
        public string Prefix { get; set; }

        [YamlMember(Alias = "cleanSession")]
        public bool CleanSession { get; set; }

        public class CredentialConfig
        {
            [YamlMember(Alias = "username")]
            public string Username { get; set; }

            [YamlMember(Alias = "password")]
            public string Password { get; set; }
        }

        public class TCPConfig
        {
            [YamlMember(Alias = "hostname")]
            public string Hostname { get; set; }

            [YamlMember(Alias = "port")]
            public int Port { get; set; }

            [YamlMember(Alias = "secure")]
            public bool Secure { get; set; }

            [YamlMember(Alias = "enabled")]
            public bool Enabled { get; set; }
        }

        public class WebSocketConfig
        {
            [YamlMember(Alias = "uri")]
            public string URI { get; set; }

            [YamlMember(Alias = "secure")]
            public bool Secure { get; set; }

            [YamlMember(Alias = "enabled")]
            public bool Enabled { get; set; }
        }

        public class LWTConfig : MsgConfig
        {
            [YamlMember(Alias = "connectMsg")]
            public string ConnectMessage { get; set; }

            [YamlMember(Alias = "disconnectMsg")]
            public string DisconnectMessage { get; set; }

            [YamlMember(Alias = "enabled")]
            public bool Enabled { get; set; }
        }

        public class MsgConfig
        {
            [YamlMember(Alias = "qos")]
            public int QoS { get; set; }

            [YamlMember(Alias = "retain")]
            public bool Retain { get; set; }
        }
    }
}
