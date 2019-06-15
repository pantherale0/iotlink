using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events.MQTT;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkService.Service.Engine.MQTT
{
    internal class MQTTClient
    {
        private static MQTTClient _instance;

        private MqttConfig _config;
        private IMqttClient _client;
        private IMqttClientOptions _options;
        private bool _connecting;
        private bool _preventReconnect;

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

        /// <summary>
        /// Initialize the MQTT Client based on the current configurations.
        /// </summary>
        /// <param name="MQTT"><see cref="MqttConfig">MQTT</see> configuration</param>
        internal void Init(MqttConfig MQTT)
        {
            // Configuration not found
            if (MQTT == null)
            {
                LoggerHelper.Error("MQTT Configuration not found.");
                return;
            }

            // No broker information
            if ((MQTT.TCP == null || !MQTT.TCP.Enabled) && (MQTT.WebSocket == null || !MQTT.WebSocket.Enabled))
            {
                LoggerHelper.Error("You need to configure TCP or WebSocket connection");
                return;
            }

            // Ambiguous broker information
            if ((MQTT.TCP != null && MQTT.TCP.Enabled) && (MQTT.WebSocket != null && MQTT.WebSocket.Enabled))
            {
                LoggerHelper.Error("You need to disable TCP or WebSocket connection. Cannot use both together.");
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
                if (string.IsNullOrWhiteSpace(_config.TCP.Hostname))
                {
                    LoggerHelper.Warn("MQTT TCP Hostname not configured yet.");
                    return;
                }

                mqttOptionBuilder = mqttOptionBuilder.WithTcpServer(_config.TCP.Hostname, _config.TCP.Port);
                if (_config.TCP.Secure)
                    mqttOptionBuilder = mqttOptionBuilder.WithTls();
            }

            // WebSocket Connection
            if (_config.WebSocket != null && _config.WebSocket.Enabled)
            {
                if (string.IsNullOrWhiteSpace(_config.WebSocket.URI))
                {
                    LoggerHelper.Warn("MQTT WebSocket URI not configured yet.");
                    return;
                }

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
                    LoggerHelper.Warn("LWT Disabled - LWT disconnected message is empty or null. Fix your configuration.yaml");
                }
            }

            // Build all options
            _options = mqttOptionBuilder.Build();

            // Create client
            _client = new MqttFactory().CreateMqttClient();
            _client.UseConnectedHandler(OnConnectedHandler);
            _client.UseDisconnectedHandler(OnDisconnectedHandler);
            _client.UseApplicationMessageReceivedHandler(OnApplicationMessageReceivedHandler);

            // Allow reconnection and go on
            Connect();
        }

        /// <summary>
        /// Publish a message to the connected broker
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        /// <param name="message">String containg the message</param>
        internal async void PublishMessage(string topic, string message)
        {
            try
            {
                if (!_client.IsConnected)
                    return;

                topic = GetFullTopicName(topic);

                LoggerHelper.Trace("Publishing to {0}: {1}", topic, message);

                MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, Encoding.UTF8.GetBytes(message), _config.Messages);
                await _client.PublishAsync(mqttMsg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to publish to {0}: {1}", topic, ex.Message);
            }
        }

        /// <summary>
        /// Publish a message to the connected broker
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        /// <param name="message">Message bytes[]</param>
        internal async void PublishMessage(string topic, byte[] message)
        {
            try
            {
                if (!_client.IsConnected)
                    return;

                topic = GetFullTopicName(topic);

                LoggerHelper.Trace("Publishing to {0}: ({1} bytes)", topic, message?.Length);

                MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, message, _config.Messages);
                await _client.PublishAsync(mqttMsg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to publish to {0}: {1}", topic, ex.Message);
            }
        }

        /// <summary>
        /// Return the current connection state
        /// </summary>
        /// <returns>Boolean</returns>
        internal bool IsConnected()
        {
            return _client != null && _client.IsConnected;
        }

        /// <summary>
        /// Disconnect from the broker
        /// </summary>
        internal void Disconnect()
        {
            try
            {
                if (!_client.IsConnected)
                    return;

                _preventReconnect = true;
                while (_client.IsConnected)
                {
                    // Send LWT Disconnected
                    if (_config.LWT != null && _config.LWT.Enabled && !string.IsNullOrWhiteSpace(_config.LWT.DisconnectMessage))
                        _client.PublishAsync(GetLWTMessage(_config.LWT.DisconnectMessage)).ConfigureAwait(false);

                    _client.DisconnectAsync().ConfigureAwait(false);
                    Thread.Sleep(250);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to disconnect. {0}", ex.Message);
            }
        }

        /// <summary>
        /// Try to connect to the configured broker.
        /// Every attempt a delay of (5 * attemps, max 60) seconds is executed.
        /// </summary>
        private async void Connect()
        {
            if (_connecting)
                return;

            _connecting = true;
            _preventReconnect = false;
            int tries = 0;
            do
            {
                LoggerHelper.Info("Trying to connect to broker: {0} (Try: {1})", GetBrokerInfo(), (tries + 1));
                try
                {
                    await _client.ConnectAsync(_options).ConfigureAwait(false);
                    LoggerHelper.Info("Connection successful");
                }
                catch (Exception)
                {
                    LoggerHelper.Info("Connection failed");
                    tries++;

                    double waitTime = Math.Min(5 * tries, 60);

                    LoggerHelper.Info("Waiting {0} seconds before trying again...", waitTime);
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));
                }
            } while (!_client.IsConnected);
            _connecting = false;
        }

        /// <summary>
        /// Handle broker connection
        /// </summary>
        /// <param name="arg"><see cref="MqttClientConnectedEventArgs"/> event</param>
        /// <returns></returns>
        private async Task OnConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            LoggerHelper.Info("MQTT Connected");

            // Send LWT Connected
            if (_config.LWT != null && _config.LWT.Enabled && !string.IsNullOrWhiteSpace(_config.LWT.ConnectMessage))
                await _client.PublishAsync(GetLWTMessage(_config.LWT.ConnectMessage)).ConfigureAwait(false);

            // Fire event
            MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Connect, arg);
            if (OnMQTTConnected != null)
                OnMQTTConnected(this, mqttEvent);

            // Subscribe to ALL Messages
            SubscribeTopic(GetFullTopicName("#"));
        }

        /// <summary>
        /// Handle broker disconnection.
        /// </summary>
        /// <param name="arg"><see cref="MqttClientDisconnectedEventArgs"/> event</param>
        /// <returns></returns>
        private async Task OnDisconnectedHandler(MqttClientDisconnectedEventArgs arg)
        {
            LoggerHelper.Info("MQTT Disconnected");

            // Fire event
            MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Disconnect, arg);
            if (OnMQTTDisconnected != null)
                OnMQTTDisconnected(this, mqttEvent);

            if (!_preventReconnect)
                Connect();
        }

        /// <summary>
        /// Handle received messages from the broker
        /// </summary>
        /// <param name="arg"><see cref="MqttApplicationMessageReceivedEventArgs"/> event</param>
        /// <returns></returns>
        private async Task OnApplicationMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            LoggerHelper.Trace("MQTT Message Received - Topic: {0}", arg.ApplicationMessage.Topic);

            // Fire event
            MQTTMessage message = GetMQTTMessage(arg);
            MQTTMessageEventEventArgs mqttEvent = new MQTTMessageEventEventArgs(MQTTEventEventArgs.MQTTEventType.MessageReceived, message, arg);
            if (OnMQTTMessageReceived != null)
                OnMQTTMessageReceived(this, mqttEvent);
        }

        /// <summary>
        /// Subscribe to a topic
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        private async void SubscribeTopic(string topic)
        {
            LoggerHelper.Debug("Subscribing to {0}", topic);

            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build()).ConfigureAwait(false);
        }

        /// <summary>
        /// Transform the <see cref="MqttApplicationMessageReceivedEventArgs"/> into <see cref="MQTTMessage"/>
        /// </summary>
        /// <param name="arg"><see cref="MqttApplicationMessageReceivedEventArgs"/> object</param>
        /// <returns><see cref="MQTTMessage"/> object</returns>
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
                        LoggerHelper.Warn("Wrong LWT QoS configuration. Defaulting to 0.");
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

            return "Unknown";
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

            string machineName = PlatformHelper.GetFullMachineName().Replace("\\", "/");
            string topic = string.Format("{0}/{1}/{2}", _config.Prefix, machineName, name);
            return MQTTHelper.SanitizeTopic(topic);
        }
    }
}
