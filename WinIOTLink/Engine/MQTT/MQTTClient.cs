using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Threading.Tasks;
using WinIOTLink.Helpers;
using static WinIOTLink.Configs.ApplicationConfig;
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
            _config = MQTT;
            var MqttOptionBuilder = new MqttClientOptionsBuilder();

            if (!String.IsNullOrEmpty(_config.ClientId))
                MqttOptionBuilder = MqttOptionBuilder.WithClientId(_config.ClientId);
            else
                MqttOptionBuilder = MqttOptionBuilder.WithClientId(Environment.MachineName);

            if (!String.IsNullOrEmpty(_config.Username))
                MqttOptionBuilder = MqttOptionBuilder.WithCredentials(_config.Username, _config.Password);

            if (_config.WebSocket && !String.IsNullOrEmpty(_config.WebSocketUrl))
                MqttOptionBuilder = MqttOptionBuilder.WithWebSocketServer(_config.WebSocketUrl);

            if (!_config.WebSocket && !String.IsNullOrEmpty(_config.Hostname))
                MqttOptionBuilder = MqttOptionBuilder.WithTcpServer(_config.Hostname, _config.Port);

            if (_config.TLSEnabled)
                MqttOptionBuilder = MqttOptionBuilder.WithTls();

            if (_config.CleanSession)
                MqttOptionBuilder = MqttOptionBuilder.WithCleanSession();

            _options = MqttOptionBuilder.Build();

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

            string fullTopic = GetFullTopicName(topic);

            LoggerHelper.Info("MQTTClient", string.Format("Publishing to {0}: {1}", fullTopic, message));
            var msg = new MqttApplicationMessageBuilder()
            .WithTopic(fullTopic)
            .WithPayload(message)
            .Build();

            await _client.PublishAsync(msg);
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
            LoggerHelper.Info("MQTTClient", string.Format("MQTT Message Received - Topic: {0}, Message: {1}", arg.ApplicationMessage.Topic, arg.ApplicationMessage.Payload));

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
        /// Return the broker information
        /// </summary>
        /// <returns>String containing the broker information</returns>
        private string GetBrokerInfo()
        {
            if (_config.WebSocket && !string.IsNullOrEmpty(_config.WebSocketUrl))
            {
                if (_config.TLSEnabled)
                    return string.Format("wss://{0}", _config.WebSocketUrl);
                else
                    return string.Format("ws://{0}", _config.WebSocketUrl);
            }

            if (!_config.WebSocket && !string.IsNullOrEmpty(_config.Hostname))
            {
                if (_config.TLSEnabled)
                    return string.Format("tls://{0}:{1}", _config.Hostname, _config.Port);
                else
                    return string.Format("tcp://{0}:{1}", _config.Hostname, _config.Port);
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
