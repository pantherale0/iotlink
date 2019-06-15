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
using System.Net;
using System.Threading.Tasks;
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

            _server = new WebSocketServer(IPAddress.Any, 9799);
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

        protected override async Task OnMessage(MessageEventArgs e)
        {
            LoggerHelper.Debug("OnMessage - 1");
            if (e.Data == null || e.Data.Length == 0)
            {
                LoggerHelper.Debug("OnMessage: e.Data == null || e.Data.Length == 0");
                return;
            }

            if (e.Opcode != Opcode.Text)
            {
                LoggerHelper.Debug("OnMessage: e.Opcode ({0}) != Opcode.Text", e.Opcode.ToString());
                return;
            }

            string data = e.Text.ReadToEnd();
            if (string.IsNullOrWhiteSpace(data))
            {
                LoggerHelper.Debug("OnMessage: Empty Payload");
                return;
            }

            LoggerHelper.Debug("OnMessage - Data: {0}", data);
            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(data);
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
                LoggerHelper.Debug("OnMessage - Exception: {0}", ex.ToString());
            }
        }

        internal void ParseClientRequest(dynamic content)
        {
            RequestTypeClient type = content.type;
            dynamic data = content.data;

            LoggerHelper.Debug("ParseClientRequest - Request Type: {0} | Data: {1}", type, data);
            switch (type)
            {
                case RequestTypeClient.REQUEST_CONNECTED:
                    ParseClientConnected(data);
                    break;

                case RequestTypeClient.REQUEST_PUBLISH_MESSAGE:
                    ParsePublishMessage(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        internal void ParseClientConnected(dynamic data)
        {
            if (!string.IsNullOrWhiteSpace(data.username))
            {
                string username = ((string)data.username).Trim().ToLowerInvariant();
                _clients[username] = Id;
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
            ResponseTypeClient type = content.type;
            dynamic data = content.data;

            LoggerHelper.Debug("ParseClientRequest - Request Type: {0} | Data: {1}", type, data);
            switch (type)
            {
                case ResponseTypeClient.RESPONSE_ADDON:
                    ParseAddonResponse(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParseAddonResponse(dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            if (!_clients.ContainsValue(Id))
                return;

            string username = _clients.First(x => x.Value == Id).Key;

            LoggerHelper.Trace("ParseAddonRequest - AgentId: {0} Username: {1} AddonId: {2} AddonData: {3}", Id, username, addonId, addonData);
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

            string payload = JsonConvert.SerializeObject(msg);
            LoggerHelper.Trace("Sending agent message: {0}", payload);

            if (username == null)
                Sessions.Broadcast(payload).ConfigureAwait(false);
            else if (_clients.ContainsKey(username))
                Sessions.SendTo(_clients[username], payload).ConfigureAwait(false);
            else
                LoggerHelper.Error("WebSocketServer - Agent from {0} not found.", username);
        }
    }
}
