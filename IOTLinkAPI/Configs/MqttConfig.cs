namespace IOTLinkAPI.Configs
{
#pragma warning disable 1591
    public class MqttConfig
    {
        public CredentialConfig Credentials { get; set; }

        public TCPConfig TCP { get; set; }

        public WebSocketConfig WebSocket { get; set; }

        public MsgConfig Messages { get; set; }

        public LWTConfig LWT { get; set; }

        public DiscoveryConfig Discovery { get; set; }

        public string ClientId { get; set; }

        public string Prefix { get; set; }

        public bool CleanSession { get; set; }

        public class CredentialConfig
        {
            public string Username { get; set; }

            public string Password { get; set; }

            public static CredentialConfig FromConfiguration(Configuration config)
            {
                CredentialConfig cfg = new CredentialConfig();
                cfg.Username = config.GetValue("username", null);
                cfg.Password = config.GetValue("password", null);

                return cfg;
            }
        }

        public class TCPConfig
        {
            public string Hostname { get; set; }

            public int Port { get; set; }

            public bool Secure { get; set; }

            public bool Enabled { get; set; }

            public static TCPConfig FromConfiguration(Configuration config)
            {
                TCPConfig cfg = new TCPConfig();
                cfg.Hostname = config.GetValue("hostname", "localhost");
                cfg.Port = config.GetValue("port", 1883);
                cfg.Secure = config.GetValue("secure", false);
                cfg.Enabled = config.GetValue("enabled", true);

                return cfg;
            }
        }

        public class WebSocketConfig
        {
            public string URI { get; set; }

            public bool Secure { get; set; }

            public bool Enabled { get; set; }

            public static WebSocketConfig FromConfiguration(Configuration config)
            {
                WebSocketConfig cfg = new WebSocketConfig();
                cfg.URI = config.GetValue("uri", null);
                cfg.Secure = config.GetValue("secure", false);
                cfg.Enabled = config.GetValue("enabled", false);

                return cfg;
            }
        }

        public class LWTConfig : MsgConfig
        {
            public string ConnectMessage { get; set; }

            public string DisconnectMessage { get; set; }

            public bool Enabled { get; set; }

            public static new LWTConfig FromConfiguration(Configuration config)
            {
                LWTConfig cfg = new LWTConfig();
                cfg.ConnectMessage = config.GetValue("connectMsg", "ON");
                cfg.DisconnectMessage = config.GetValue("disconnectMsg", "OFF");
                cfg.QoS = config.GetValue("qos", 1);
                cfg.Retain = config.GetValue("retain", false);
                cfg.Enabled = config.GetValue("enabled", true);

                return cfg;
            }
        }

        public class MsgConfig
        {
            public int QoS { get; set; }

            public bool Retain { get; set; }

            public static MsgConfig FromConfiguration(Configuration config)
            {
                MsgConfig cfg = new MsgConfig();
                cfg.QoS = config.GetValue("qos", 1);
                cfg.Retain = config.GetValue("retain", false);

                return cfg;
            }
        }

        public class DiscoveryConfig
        {
            public string TopicPrefix { get; set; }

            public bool Enabled { get; set; }

            public bool DomainPrefix { get; set; }

            public static new DiscoveryConfig FromConfiguration(Configuration config)
            {
                DiscoveryConfig cfg = new DiscoveryConfig();
                cfg.TopicPrefix = config.GetValue("topicPrefix", "homeassistant");
                cfg.DomainPrefix = config.GetValue("domainPrefix", false);
                cfg.Enabled = config.GetValue("enabled", true);

                return cfg;
            }
        }

        public static MqttConfig FromConfiguration(Configuration config)
        {
            MqttConfig mqtt = new MqttConfig();
            mqtt.ClientId = config.GetValue("clientId", null);
            mqtt.CleanSession = config.GetValue("cleanSession", true);
            mqtt.Prefix = config.GetValue("prefix", "IOTLink");
            mqtt.Credentials = CredentialConfig.FromConfiguration(config.GetValue("credentials"));
            mqtt.TCP = TCPConfig.FromConfiguration(config.GetValue("tcp"));
            mqtt.WebSocket = WebSocketConfig.FromConfiguration(config.GetValue("websocket"));
            mqtt.Messages = MsgConfig.FromConfiguration(config.GetValue("messages"));
            mqtt.LWT = LWTConfig.FromConfiguration(config.GetValue("lwt"));
            mqtt.Discovery = DiscoveryConfig.FromConfiguration(config.GetValue("discovery"));

            return mqtt;
        }
    }
}
