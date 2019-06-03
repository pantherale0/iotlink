using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading.Tasks;
using WinIOTLink.Configs;
using WinIOTLink.Helpers;
using static WinIOTLink.Engine.MQTT.MQTTHandlers;

namespace WinIOTLink.Engine.MQTT
{
    internal class MQTTClient
    {
        private static MQTTClient _instance;

        private MqttConfig _config;
        private IMqttClient _client;
        private IMqttClientOptions _options;
        private bool _connecting;

        public event MQTTEventHandler OnMQTTConnected;
        public event MQTTEventHandler OnMQTTDisconnected;
        public event MQTTMessageEventHandler OnMQTTMessageReceived;

        public static MQTTClient GetInstance()
        {
            if (_instance == null)
                _instance = new MQTTClient();

            return _instance;
        }

        private MQTTClient()
        {

        }

        public void Init(MqttConfig MQTT)
        {
            // Configuration not found
            if (MQTT == null)
            {
                LoggerHelper.Error("MQTTClient", "MQTT Configuration not found.");
                return;
            }

            // No broker information
            if ((MQTT.TCP == null || !MQTT.TCP.Enabled) && (MQTT.WebSocket == null || !MQTT.WebSocket.Enabled))
            {
                LoggerHelper.Error("MQTTClient", "You need to configure TCP or WebSocket connection");
                return;
            }

            // Ambiguous broker information
            if ((MQTT.TCP != null && MQTT.TCP.Enabled) && (MQTT.WebSocket != null && MQTT.WebSocket.Enabled))
            {
                LoggerHelper.Error("MQTTClient", "You need to disable TCP or WebSocket connection. Cannot use both together.");
                return;
            }


            _config = MQTT;
            var mqttOptionBuilder = new MqttClientOptionsBuilder();

            // Credentials
            if (_config.Credentials != null && !string.IsNullOrWhiteSpace(_config.Credentials.Username))
            {
                mqttOptionBuilder = mqttOptionBuilder.WithCredentials(_config.Credentials.Username, _config.Credentials.Password);
            }

            // TCP Connection
            if (_config.TCP != null && _config.TCP.Enabled)
            {
                mqttOptionBuilder = mqttOptionBuilder.WithTcpServer(_config.TCP.Hostname, _config.TCP.Port);
                if (_config.TCP.Secure)
                    mqttOptionBuilder = mqttOptionBuilder.WithTls();
            }

            // WebSocket Connection
            if (_config.WebSocket != null && _config.WebSocket.Enabled)
            {
                mqttOptionBuilder = mqttOptionBuilder.WithWebSocketServer(_config.WebSocket.URI);
                if (_config.TCP.Secure)
                    mqttOptionBuilder = mqttOptionBuilder.WithTls();
            }

            // Client ID
            if (!String.IsNullOrEmpty(_config.ClientId))
                mqttOptionBuilder = mqttOptionBuilder.WithClientId(_config.ClientId);
            else
                mqttOptionBuilder = mqttOptionBuilder.WithClientId(Environment.MachineName);

            // Clean Session
            if (_config.CleanSession)
                mqttOptionBuilder = mqttOptionBuilder.WithCleanSession();

            // LWT
            if (_config.LWT != null && _config.LWT.Enabled)
            {
                if (!string.IsNullOrWhiteSpace(_config.LWT.DisconnectMessage))
                {
                    mqttOptionBuilder = mqttOptionBuilder.WithWillMessage(GetLWTMessage(_config.LWT.DisconnectMessage));
                }
                else
                {
                    LoggerHelper.Warn("MQTTClient", "LWT Disabled - LWT disconnected message is empty or null. Fix your configuration.yaml");
                }
            }

            // Build all options
            _options = mqttOptionBuilder.Build();

            // Create client
            _client = new MqttFactory().CreateMqttClient();
            _client.UseConnectedHandler(OnConnectedHandler);
            _client.UseDisconnectedHandler(OnDisconnectedHandler);
            _client.UseApplicationMessageReceivedHandler(OnApplicationMessageReceivedHandler);
            Connect();
        }

        public async void PublishMessage(string topic, string message)
        {
            if (!_client.IsConnected)
                return;

            topic = GetFullTopicName(topic);

            LoggerHelper.Info("MQTTClient", string.Format("Publishing to {0}: {1}", topic, message));

            MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, Encoding.UTF8.GetBytes(message), _config.Messages);
            await _client.PublishAsync(mqttMsg);
        }

        public async void PublishMessage(string topic, byte[] message)
        {
            if (!_client.IsConnected)
                return;

            topic = GetFullTopicName(topic);

            LoggerHelper.Info("MQTTClient", string.Format("Publishing to {0}: ({1} bytes)", topic, message?.Length));

            MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, message, _config.Messages);
            await _client.PublishAsync(mqttMsg);
        }

        public bool isConnected()
        {
            return _client.IsConnected;
        }

        private async void Connect()
        {
            if (_connecting)
                return;

            _connecting = true;
            int tries = 0;
            do
            {
                LoggerHelper.Info("MQTTClient", string.Format("Trying to connect to broker: {0} (Try: {1})", GetBrokerInfo(), (tries + 1)));
                try
                {
                    await _client.ConnectAsync(_options);
                    LoggerHelper.Info("MQTTClient", "Connection successful");
                }
                catch
                {
                    LoggerHelper.Info("MQTTClient", "Connection failed");
                    tries++;

                    double waitTime = Math.Min(10 * tries, 300);

                    LoggerHelper.Info("MQTTClient", string.Format("Waiting {0} seconds before trying again...", waitTime));
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));
                }
            } while (!_client.IsConnected);
            _connecting = false;
        }

        private async Task OnConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            LoggerHelper.Info("MQTTClient", "MQTT Connected");

            // Send LWT Connected
            if (_config.LWT != null && !string.IsNullOrWhiteSpace(_config.LWT.ConnectMessage))
                await _client.PublishAsync(GetLWTMessage(_config.LWT.ConnectMessage));

            // Fire event
            MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Connect, arg);
            if (OnMQTTConnected != null)
                OnMQTTConnected(this, mqttEvent);

            // Subscribe to ALL Messages
            SubscribeTopic(GetFullTopicName("#"));
        }

        private async Task OnDisconnectedHandler(MqttClientDisconnectedEventArgs arg)
        {
            LoggerHelper.Info("MQTTClient", "MQTT Disconnected");

            // Fire event
            MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Disconnect, arg);
            if (OnMQTTDisconnected != null)
                OnMQTTDisconnected(this, mqttEvent);

            Connect();
        }

        private async Task OnApplicationMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            LoggerHelper.Info("MQTTClient", string.Format("MQTT Message Received - Topic: {0}", arg.ApplicationMessage.Topic));

            // Fire event
            MQTTMessage message = GetMQTTMessage(arg);
            MQTTMessageEventEventArgs mqttEvent = new MQTTMessageEventEventArgs(MQTTEventEventArgs.MQTTEventType.MessageReceived, message, arg);
            if (OnMQTTMessageReceived != null)
                OnMQTTMessageReceived(this, mqttEvent);
        }

        private async void SubscribeTopic(string topic)
        {
            LoggerHelper.Info("MQTTClient", string.Format("Subscribing to {0}", topic));

            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        private MQTTMessage GetMQTTMessage(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (arg == null)
                return null;

            MqttApplicationMessage mqttMsg = arg.ApplicationMessage;
            MQTTMessage message = new MQTTMessage
            {
                FullTopic = mqttMsg.Topic,
                Topic = GetMessageTopic(mqttMsg.Topic),
                ContentType = mqttMsg.ContentType,
                Retain = mqttMsg.Retain,
                Payload = mqttMsg.Payload
            };

            return message;
        }

        /// <summary>
        /// Create the LWT message
        /// </summary>
        /// <returns>LWT message</returns>
        private MqttApplicationMessage GetLWTMessage(string message)
        {
            string topic = GetFullTopicName("LWT");
            return BuildMQTTMessage(topic, Encoding.UTF8.GetBytes(message), _config.LWT);
        }

        /// <summary>
        /// Build MQTT message ready to be published based on user configurations
        /// </summary>
        /// <param name="topic">Full topic name</param>
        /// <param name="payload">Payload (bytes[])</param>
        /// <param name="msgConfig">Configuration to be used (optional)</param>
        /// <param name="builder">Builder instance to be used (optional)</param>
        /// <returns>MqttApplicationMessage instance ready to be sent</returns>
        private MqttApplicationMessage BuildMQTTMessage(string topic, byte[] payload, MqttConfig.MsgConfig msgConfig = null, MqttApplicationMessageBuilder builder = null)
        {
            if (builder == null)
                builder = new MqttApplicationMessageBuilder();

            // Topic and Payload
            builder = builder.WithTopic(topic);
            builder = builder.WithPayload(payload);

            if (msgConfig != null)
            {
                // Retain
                builder = builder.WithRetainFlag(msgConfig.Retain);

                // QoS
                switch (msgConfig.QoS)
                {
                    case 0:
                        builder = builder.WithAtMostOnceQoS();
                        break;
                    case 1:
                        builder = builder.WithAtLeastOnceQoS();
                        break;
                    case 2:
                        builder = builder.WithExactlyOnceQoS();
                        break;
                    default:
                        LoggerHelper.Warn("MQTTClient", "Wrong LWT QoS configuration. Defaulting to 0.");
                        builder = builder.WithAtMostOnceQoS();
                        break;
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Return the broker information
        /// </summary>
        /// <returns>String containing the broker information</returns>
        private string GetBrokerInfo()
        {
            if (_config.WebSocket != null && _config.WebSocket.Enabled)
            {
                if (_config.WebSocket.Secure)
                    return string.Format("wss://{0}", _config.WebSocket.URI);
                else
                    return string.Format("ws://{0}", _config.WebSocket.URI);
            }

            if (_config.TCP != null && _config.TCP.Enabled)
            {
                if (_config.TCP.Secure)
                    return string.Format("tls://{0}:{1}", _config.TCP.Hostname, _config.TCP.Port);
                else
                    return string.Format("tcp://{0}:{1}", _config.TCP.Hostname, _config.TCP.Port);
            }

            return "unknown";
        }

        /// <summary>
        /// Return message topic string
        /// </summary>
        /// <param name="topic">Topic name</param>
        /// <returns>String containing the topic string</returns>
        private string GetMessageTopic(string topic)
        {
            if (topic == null)
                return string.Empty;

            return MQTTHelper.SanitizeTopic(topic).Replace(GetFullTopicName(), "");
        }

        /// <summary>
        /// Return the full topic name (including domain/machine)
        /// </summary>
        /// <param name="name">message topic string</param>
        /// <returns>String containing the full topic name</returns>
        private string GetFullTopicName(string name = "")
        {
            if (name == null)
                name = string.Empty;

            string machineName = WindowsHelper.GetFullMachineName().Replace("\\", "/");
            string topic = string.Format("{0}/{1}/{2}", _config.Prefix, machineName, name);
            return MQTTHelper.SanitizeTopic(topic);
        }
    }
}
