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
            LoggerHelper.Trace("MQTTClient instance created.");
        }

        /// <summary>
        /// Initialize the MQTT Client.
        /// </summary>
        /// <returns>Boolean</returns>
        internal bool Init()
        {
            _config = ConfigHelper.GetEngineConfig().MQTT;

            // Configuration not found
            if (_config == null)
            {
                LoggerHelper.Warn("MQTT is disabled or not configured yet.");
                return false;
            }

            // No broker information
            if ((_config.TCP == null || !_config.TCP.Enabled) && (_config.WebSocket == null || !_config.WebSocket.Enabled))
            {
                LoggerHelper.Error("You need to configure TCP or WebSocket connection");
                return false;
            }

            // Ambiguous broker information
            if ((_config.TCP != null && _config.TCP.Enabled) && (_config.WebSocket != null && _config.WebSocket.Enabled))
            {
                LoggerHelper.Error("You need to disable TCP or WebSocket connection. Cannot use both together.");
                return false;
            }

            var mqttOptionBuilder = new MqttClientOptionsBuilder();

            // Credentials
            if (_config.Credentials != null && !string.IsNullOrWhiteSpace(this._config.Credentials.Username))
                mqttOptionBuilder = mqttOptionBuilder.WithCredentials(this._config.Credentials.Username, this._config.Credentials.Password);

            // TCP Connection
            if (_config.TCP != null && _config.TCP.Enabled)
            {
                if (string.IsNullOrWhiteSpace(_config.TCP.Hostname))
                {
                    LoggerHelper.Warn("MQTT TCP Hostname not configured yet.");
                    return false;
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
                    return false;
                }

                mqttOptionBuilder = mqttOptionBuilder.WithWebSocketServer(_config.WebSocket.URI);
                if (_config.TCP.Secure)
                    mqttOptionBuilder = mqttOptionBuilder.WithTls();
            }

            // Client ID
            if (!string.IsNullOrEmpty(_config.ClientId))
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

            LoggerHelper.Trace("MQTT Init finished.");
            return true;
        }

        /// <summary>
        /// Try to connect to the configured broker.
        /// Every attempt a delay of (5 * attemps, max 60) seconds is executed.
        /// </summary>
        internal async void Connect()
        {
            if (_connecting)
            {
                LoggerHelper.Verbose("MQTT client is already connecting. Skipping");
                return;
            }

            int tries = 0;
            _connecting = true;
            _preventReconnect = false;

            do
            {
                // Safely disconnect the existing client if it exists.
                try
                {
                    if (_client != null && _client.IsConnected)
                    {
                        LoggerHelper.Verbose("Disconnecting from previous session.");
                        Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("Error while trying to disconnect an existing MQTT Client: {0}", ex.ToString());
                }
                finally
                {
                    _client = null;
                }

                // Safely again, try to connect with the existing broker information.
                try
                {
                    LoggerHelper.Info("Trying to connect to broker: {0} (Try: {1}).", GetBrokerInfo(), (tries + 1));

                    _client = new MqttFactory().CreateMqttClient();
                    _client.UseConnectedHandler(OnConnectedHandler);
                    _client.UseDisconnectedHandler(OnDisconnectedHandler);
                    _client.UseApplicationMessageReceivedHandler(OnApplicationMessageReceivedHandler);

                    await _client.ConnectAsync(_options).ConfigureAwait(false);

                    LoggerHelper.Info("Connection established successfully.");
                }
                catch (Exception ex)
                {
                    LoggerHelper.Info("Connection failed: {0}", ex.ToString());
                    tries++;

                    double waitTime = Math.Min(5 * tries, 60);

                    LoggerHelper.Info("Waiting {0} seconds before trying again...", waitTime);
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));
                }
            } while (!_client.IsConnected);
            _connecting = false;
        }

        /// <summary>
        /// Disconnect from the broker
        /// </summary>
        internal void Disconnect(bool skipLastWill = false)
        {
            try
            {
                if (_client == null)
                    return;

                if (!_client.IsConnected)
                {
                    LoggerHelper.Verbose("MQTT Client not connected. Skipping.");
                    _client = null;
                    return;
                }

                int tries = 0;
                _preventReconnect = true;

                LoggerHelper.Verbose("Disconnecting from MQTT Broker.");
                while (_client.IsConnected)
                {
                    LoggerHelper.Trace("Trying to disconnect from the broker (Try: {0}).", tries++);

                    // Send LWT Disconnected
                    if (!skipLastWill && IsLastWillEnabled() && !string.IsNullOrWhiteSpace(_config.LWT.DisconnectMessage))
                    {
                        LoggerHelper.Verbose("Sending LWT message before disconnecting.");
                        _client.PublishAsync(GetLWTMessage(_config.LWT.DisconnectMessage)).ConfigureAwait(false);
                    }

                    _client.DisconnectAsync().ConfigureAwait(false);
                    Task.Delay(TimeSpan.FromSeconds(5));
                }

                // Remove client reference.
                _client = null;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to disconnect. {0}", ex.Message);
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
        /// Return if LWT is enabled
        /// </summary>
        /// <returns></returns>
        internal bool IsLastWillEnabled()
        {
            return _config.LWT != null && _config.LWT.Enabled;
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
                if (_client == null || !_client.IsConnected)
                {
                    LoggerHelper.Verbose("MQTT Client not connected. Skipping.");
                    return;
                }

                topic = GetFullTopicName(topic);

                LoggerHelper.Trace("Publishing to {0}: {1}", topic, message);

                MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, Encoding.UTF8.GetBytes(message), _config.Messages);
                await _client.PublishAsync(mqttMsg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to publish to {0}: {1}", topic, ex.Message);
                if (!_preventReconnect)
                {
                    LoggerHelper.Verbose("Reconnecting...");
                    Connect();
                }
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
                if (_client == null || !_client.IsConnected)
                {
                    LoggerHelper.Verbose("MQTT Client not connected. Skipping.");
                    return;
                }

                topic = GetFullTopicName(topic);

                LoggerHelper.Trace("Publishing to {0}: ({1} bytes)", topic, message?.Length);

                MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, message, _config.Messages);
                await _client.PublishAsync(mqttMsg).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to publish to {0}: {1}", topic, ex.Message);
                if (!_preventReconnect)
                {
                    LoggerHelper.Verbose("Reconnecting...");
                    Connect();
                }
            }
        }

        /// <summary>
        /// Handle broker connection
        /// </summary>
        /// <param name="arg"><see cref="MqttClientConnectedEventArgs"/> event</param>
        /// <returns></returns>
        private async Task OnConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            LoggerHelper.Verbose("MQTT Connected");
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Send LWT Connected
            if (IsLastWillEnabled() && !string.IsNullOrWhiteSpace(_config.LWT.ConnectMessage))
                await _client.PublishAsync(GetLWTMessage(_config.LWT.ConnectMessage)).ConfigureAwait(false);

            // Fire event
            MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Connect, arg);
            OnMQTTConnected?.Invoke(this, mqttEvent);

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
            LoggerHelper.Verbose("MQTT Disconnected");
            await Task.Delay(TimeSpan.FromSeconds(1));

            // Fire event
            MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Disconnect, arg);
            OnMQTTDisconnected?.Invoke(this, mqttEvent);

            if (!_preventReconnect)
            {
                LoggerHelper.Verbose("Reconnecting...");
                Connect();
            }
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
            OnMQTTMessageReceived?.Invoke(this, mqttEvent);
        }

        /// <summary>
        /// Subscribe to a topic
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        private async void SubscribeTopic(string topic)
        {
            LoggerHelper.Trace("Subscribing to {0}", topic);
            try
            {
                await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to subscribe to {0}: {1}", topic, ex.ToString());
                if (!_preventReconnect)
                {
                    LoggerHelper.Verbose("Reconnecting...");
                    Connect();
                }
            }
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
