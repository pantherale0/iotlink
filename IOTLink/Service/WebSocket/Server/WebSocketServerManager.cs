using IOTLink.Platform.WebSocket;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkService.Engine.MQTT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace IOTLink.Service.WSServer
{
    internal class WebSocketServerManager : WebSocketBehavior
    {
        public const string WEBSOCKET_URI = "ws://localhost:9799";
        private static WebSocketServerManager _instance;

        private WebSocketServer _server;

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
            _server.KeepClean = true;
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
            if (e.RawData == null || e.RawData.Length == 0 || string.IsNullOrWhiteSpace(e.Data))
                return;

            if (e.IsPing || !e.IsText)
                return;

            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(e.Data);
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
            catch (Exception)
            {

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
                    SendRequest(RequestTypeServer.REQUEST_DISPLAY_INFO);
                    SendRequest(RequestTypeServer.REQUEST_IDLE_TIME);
                    break;

                case RequestTypeClient.REQUEST_PUBLISH_MESSAGE:
                    string topic = data.topic;
                    byte[] payload = data.payload;
                    MQTTClient.GetInstance().PublishMessage(topic, payload);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        internal void ParseClientResponse(dynamic content)
        {
            ResponseTypeClient type = content.type;
            dynamic data = content.data;

            LoggerHelper.Debug("ParseClientRequest - Request Type: {0} | Data: {1}", type, data);
            switch (type)
            {
                case ResponseTypeClient.RESPONSE_DISPLAY_INFO:
                    List<DisplayInfo> displays = content;
                    LoggerHelper.Debug("ParseClientResponse - Display Infos: {0}", displays);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        internal void ParseAPIMessage(dynamic content)
        {
            LoggerHelper.Debug("ParseAPIMessage: {0}", content);
        }

        internal void SendRequest(RequestTypeServer type, dynamic data = null)
        {
            if (!IsConnected())
                return;

            dynamic msg = new ExpandoObject();
            msg.messageType = MessageType.SERVER_REQUEST;
            msg.content = new ExpandoObject();
            msg.content.type = type;
            msg.content.data = data;

            Sessions.Broadcast(JsonConvert.SerializeObject(msg));
        }
    }
}
