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
using WebSocketSharp;
using WebSocketSharp.Server;

namespace IOTLinkService.Service.WSServer
{
    internal class WebSocketServerManager : WebSocketBehavior
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

        }

        internal void Init()
        {
            if (_server != null)
                Disconnect();

            _server = new WebSocketServer(WEBSOCKET_URI);
            _server.AddWebSocketService("/", () => this);
            _server.Start();
        }

        internal void Disconnect()
        {
            if (_server == null)
                return;

            if (_server.IsListening)
                _server.Stop();

            _server = null;
        }

        internal bool IsConnected()
        {
            return _server != null && _server.IsListening;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsPing)
            {
                LoggerHelper.Trace("OnMessage - Ping received from {0}", ID);
                Sessions.PingTo(ID);
                return;
            }

            if (e.RawData == null || e.RawData.Length == 0 || !e.IsText || string.IsNullOrWhiteSpace(e.Data))
            {
                LoggerHelper.Trace("OnMessage: Invalid message content [1].");
                return;
            }

            string data = e.Data;
            LoggerHelper.Trace("Message received from client {0}: {1}", ID, data);
            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(data);
                if (json == null || json.messageType == null || json.content == null)
                {
                    LoggerHelper.Trace("OnMessage: Invalid message content [2].");
                    return;
                }

                MessageType messageType = json.messageType;
                switch (messageType)
                {
                    case MessageType.CLIENT_REQUEST:
                        ParseClientRequest(json.content);
                        break;

                    case MessageType.CLIENT_RESPONSE:
                        ParseClientResponse(json.content);
                        break;

                    case MessageType.API_MESSAGE:
                        ParseAPIMessage(json.content);
                        break;

                    default:
                        LoggerHelper.Warn("WebSocket client send an invalid message type ({0}) with data: {1}", json.messageType, json.data);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnMessage: Exception: {0}", ex.ToString());
            }
        }

        internal void ParseClientRequest(dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseClientRequest: Invalid message content.");
                return;
            }

            RequestTypeClient type = content.type;
            dynamic data = content.data;
            switch (type)
            {
                case RequestTypeClient.REQUEST_CONNECTED:
                    ParseClientConnected(data);
                    break;

                case RequestTypeClient.REQUEST_PUBLISH_MESSAGE:
                    ParsePublishMessage(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest: Invalid Request Type: {0}", type);
                    break;
            }
        }

        internal void ParseClientConnected(dynamic data)
        {
            string username = data.username;
            if (!string.IsNullOrWhiteSpace(username))
            {
                username = username.Trim().ToLowerInvariant();
                _clients[username] = ID;
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

        internal void ParseClientResponse(dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseClientResponse: Invalid message content.");
                return;
            }

            ResponseTypeClient type = content.type;
            dynamic data = content.data;
            switch (type)
            {
                case ResponseTypeClient.RESPONSE_ADDON:
                    ParseAddonResponse(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientResponse: Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParseAddonResponse(dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            if (!_clients.ContainsValue(ID))
                return;

            string username = _clients.First(x => x.Value == ID).Key;
            ServiceAddonManager.GetInstance().Raise_OnAgentResponse(username, addonId, addonData);
        }

        internal void ParseAPIMessage(dynamic content)
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
            LoggerHelper.Trace("Sending message to clients: {0}", payload);

            if (username == null)
                Sessions.Broadcast(payload);
            else if (_clients.ContainsKey(username))
                Sessions.SendTo(_clients[username], payload);
            else
                LoggerHelper.Error("WebSocketServer - Agent from {0} not found.", username);
        }
    }
}
