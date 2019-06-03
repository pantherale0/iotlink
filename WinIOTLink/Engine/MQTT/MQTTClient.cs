using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Threading.Tasks;
using WinIOTLink.Helpers;
using static WinIOTLink.Configs.ApplicationConfig;
using static WinIOTLink.Helpers.LoggerHelper;

namespace WinIOTLink.Engine.MQTT
{
    class MQTTClient
    {
        private static MQTTClient _instance;

        private MqttConfig _config;
        private IMqttClient _client;
        private IMqttClientOptions _options;
        private bool _connecting;

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
            _client.UseConnectedHandler(OnMQTTConnected);
            _client.UseDisconnectedHandler(OnMQTTDisconnect);
            _client.UseApplicationMessageReceivedHandler(OnMQTTMessageReceived);
            Connect();
        }

        public async void PublishMessage(string topic, string message)
        {
            if (!_client.IsConnected)
                return;

            String fullTopic = GetFullTopicName(topic);

            LoggerHelper.WriteToFile("MQTTClient", String.Format("Publishing to {0}: {1}", fullTopic, message), LogLevel.INFO);
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
                LoggerHelper.WriteToFile("MQTTClient", String.Format("Trying to connect to broker: {0} (Try: {1})", GetBrokerInfo(), (tries + 1)), LogLevel.INFO);
                try
                {
                    await _client.ConnectAsync(_options);
                    LoggerHelper.WriteToFile("MQTTClient", "Reconnection successful", LogLevel.INFO);
                }
                catch
                {
                    LoggerHelper.WriteToFile("MQTTClient", "Reconnection failed", LogLevel.INFO);
                    tries++;

                    double waitTime = Math.Min(10 * tries, 300);

                    LoggerHelper.WriteToFile("MQTTClient", String.Format("Waiting {0} seconds before trying again...", waitTime), LogLevel.INFO);
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));
                }
            } while (!_client.IsConnected);
            _connecting = false;
        }

        private async Task OnMQTTConnected(MqttClientConnectedEventArgs arg)
        {
            LoggerHelper.WriteToFile("MQTTClient", "MQTT Connected", LogLevel.INFO);
            MQTTEvent mqttEvent = new MQTTEvent(MQTTEvent.MQTTEventType.Connect, arg);

            // Subscribe to ALL Messages
            SubscribeTopic(GetFullTopicName("#"));
        }

        private async void SubscribeTopic(string topic)
        {
            LoggerHelper.WriteToFile("MQTTClient", String.Format("Subscribing to {0}", topic), LogLevel.INFO);

            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        private async Task OnMQTTDisconnect(MqttClientDisconnectedEventArgs arg)
        {
            LoggerHelper.WriteToFile("MQTTClient", "MQTT Disconnected", LogLevel.INFO);
            MQTTEvent mqttEvent = new MQTTEvent(MQTTEvent.MQTTEventType.Disconnect, arg);
            Connect();
        }

        private async Task OnMQTTMessageReceived(MqttApplicationMessageReceivedEventArgs arg)
        {
            LoggerHelper.WriteToFile("MQTTClient", String.Format("MQTT Message Received - Topic: {0}, Message: {1}", arg.ApplicationMessage.Topic, arg.ApplicationMessage.Payload), LogLevel.INFO);

            MQTTMessage message = GetMQTTMessage(arg);
            MQTTEvent mqttEvent = new MQTTEvent(MQTTEvent.MQTTEventType.MessageReceived, message, arg);
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
            if (_config.WebSocket && !String.IsNullOrEmpty(_config.WebSocketUrl))
            {
                if (_config.TLSEnabled)
                    return String.Format("wss://{0}", _config.WebSocketUrl);
                else
                    return String.Format("ws://{0}", _config.WebSocketUrl);
            }

            if (!_config.WebSocket && !String.IsNullOrEmpty(_config.Hostname))
            {
                if (_config.TLSEnabled)
                    return String.Format("tls://{0}:{1}", _config.Hostname, _config.Port);
                else
                    return String.Format("tcp://{0}:{1}", _config.Hostname, _config.Port);
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
                return String.Empty;

            return topic.Replace("\\\\", "\\").Replace(GetFullTopicName(), "");
        }

        /// <summary>
        /// Return the full topic name (including domain/machine)
        /// </summary>
        /// <param name="name">message topic string</param>
        /// <returns>String containing the full topic name</returns>
        private string GetFullTopicName(string name = "")
        {
            if (name == null)
                name = "";

            if (name[0] == '/')
                name = name.Remove(0, 1);

            string machineTopic = SanitizeForMqtt(WindowsHelper.GetFullMachineName());
            return String.Format("{0}/{1}/{2}", SanitizeForMqtt(_config.Prefix), machineTopic, SanitizeForMqtt(name));
        }

        /// <summary>
        /// Sanitize the string to be used in mqtt topics
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>String correctly cleaned</returns>
        private string SanitizeForMqtt(string str)
        {
            if (str == null)
                return String.Empty;

            return StringHelper.RemoveDiacritics(str)
                .Replace(' ', '_')
                .Replace('\\', '/');
        }
    }
}
