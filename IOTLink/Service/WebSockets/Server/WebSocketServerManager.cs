using IOTLink.Platform.WebSocket;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkService.Service.Engine;
using IOTLinkService.Service.Engine.MQTT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace IOTLinkService.Service.WebSockets.Server
{
    internal class WebSocketServerManager
    {
        public const string WEBSOCKET_URI = "ws://localhost:9799";
        private static WebSocketServerManager _instance;

        private WebSocketServer _server;

        private Dictionary<string, string> _clients = new Dictionary<string, string>();

        public static WebSocketServerManager GetInstance()
        {
            if (_instance == null)
                _instance = new WebSocketServerManager();

            return _instance;
        }

        private WebSocketServerManager()
        {
            LoggerHelper.Trace("WebSocketServerManager instance created.");
        }

        internal void Init()
        {
            if (_server != null)
            {
                LoggerHelper.Verbose("WebSocketServer instance found. Disconnecting.");
                Disconnect();
            }

            _server = new WebSocketServer();
            _server.OnMessageHandler += OnMessage;
            _server.Start(WEBSOCKET_URI);
        }

        internal void Disconnect()
        {
            if (_server == null)
                return;

            _server.Disconnect();
            _server = null;
        }

        internal bool IsConnected()
        {
            return _server != null;
        }

        protected void OnMessage(object sender, WebSocketMessageEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message))
                {
                    LoggerHelper.Trace("OnMessage - Empty message received.");
                    return;
                }

                LoggerHelper.Trace("Message received from client {0}", e.ID);
                LoggerHelper.DataDump("Message Payload: {0}", e.Message);

                dynamic json = JsonConvert.DeserializeObject<dynamic>(e.Message);
                if (json == null || json.messageType == null || json.content == null)
                {
                    LoggerHelper.Trace("OnMessage - Invalid message content.");
                    return;
                }

                MessageType messageType = json.messageType;
                switch (messageType)
                {
                    case MessageType.CLIENT_REQUEST:
                        ParseClientRequest(e.ID, json.content);
                        break;

                    case MessageType.CLIENT_RESPONSE:
                        ParseClientResponse(e.ID, json.content);
                        break;

                    case MessageType.API_MESSAGE:
                        ParseAPIMessage(e.ID, json.content);
                        break;

                    default:
                        LoggerHelper.Warn("WebSocket client send an invalid message type ({0}) with data: {1}", json.messageType, json.data);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnMessage - Exception: {0}", ex.ToString());
            }
        }

        internal void ParseClientRequest(string clientId, dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseClientRequest - Invalid message content.");
                return;
            }

            RequestTypeClient type = content.type;
            dynamic data = content.data;
            switch (type)
            {
                case RequestTypeClient.REQUEST_CONNECTED:
                    ParseClientConnected(clientId, data);
                    break;

                case RequestTypeClient.REQUEST_PUBLISH_MESSAGE:
                    ParsePublishMessage(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        internal void ParseClientConnected(string clientId, dynamic data)
        {
            string username = data.username;
            if (!string.IsNullOrWhiteSpace(username))
            {
                username = username.Trim().ToLowerInvariant();
                _clients[username] = clientId;
            }
        }

        internal void ParsePublishMessage(dynamic data)
        {
            string topic = data.topic;
            byte[] payload = data.payload;

            if (string.IsNullOrWhiteSpace(topic) || payload == null || payload.Length == 0)
                return;

            MQTTClient.GetInstance().PublishMessage(topic, payload);
        }

        internal void ParseClientResponse(string clientId, dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseClientResponse - Invalid message content.");
                return;
            }

            ResponseTypeClient type = content.type;
            dynamic data = content.data;
            switch (type)
            {
                case ResponseTypeClient.RESPONSE_ADDON:
                    ParseAddonResponse(clientId, data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientResponse - Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParseAddonResponse(string clientId, dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            if (!_clients.ContainsValue(clientId))
                return;

            string username = _clients.First(x => x.Value == clientId).Key;
            ServiceAddonManager.GetInstance().Raise_OnAgentResponse(username, addonId, addonData);
        }

        internal void ParseAPIMessage(string clientId, dynamic content)
        {
            LoggerHelper.Debug("ParseAPIMessage: {0}", content);
        }

        internal void SendRequest(RequestTypeServer type, dynamic data = null, string username = null)
        {
            if (!IsConnected())
                return;

            if (string.IsNullOrWhiteSpace(username))
                username = null;
            else
                username = username.Trim().ToLowerInvariant();

            dynamic msg = new ExpandoObject();
            msg.messageType = MessageType.SERVER_REQUEST;
            msg.content = new ExpandoObject();
            msg.content.type = type;
            msg.content.data = data;

            string payload = JsonConvert.SerializeObject(msg, Formatting.None);
            LoggerHelper.Verbose("Sending message to clients");
            LoggerHelper.DataDump("Message Payload: {0}", payload);

            if (username == null)
                _server.Broadcast(payload);
            else if (_clients.ContainsKey(username))
                _server.SendMessage(_clients[username], payload);
            else
                LoggerHelper.Warn("WebSocketServer - Agent from {0} not found.", username);
        }
    }
}
