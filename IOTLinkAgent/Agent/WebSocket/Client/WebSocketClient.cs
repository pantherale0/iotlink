using IOTLink.Platform.WebSocket;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkAPI.Platform.Windows;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using WebSocketSharp;

namespace IOTLinkAgent.Agent.WSClient
{
    internal class WebSocketClient
    {
        private static WebSocketClient _instance;

        private WebSocket _client;

        public static WebSocketClient GetInstance()
        {
            if (_instance == null)
                _instance = new WebSocketClient();

            return _instance;
        }

        private WebSocketClient()
        {

        }

        internal void Init()
        {
            if (_client != null)
                Disconnect();

            _client = new WebSocket("ws://localhost:9799/");
            _client.OnMessage += OnMessageReceived;
            _client.Connect();

            SendRequest(RequestTypeClient.REQUEST_CONNECTED);
        }

        internal void Disconnect()
        {
            if (_client == null)
                return;

            if (_client.IsAlive)
                _client.Close();

            _client = null;
        }

        internal bool IsConnected()
        {
            return _client != null && _client.IsAlive;
        }

        internal void SendRequest(RequestTypeClient type, dynamic data = null)
        {
            SendMessage(MessageType.CLIENT_REQUEST, type, data);
        }

        internal void SendResponse(ResponseTypeClient type, dynamic data = null)
        {
            SendMessage(MessageType.CLIENT_RESPONSE, type, data);
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
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
                    case MessageType.SERVER_REQUEST:
                        ParseServerRequest(json.content);
                        break;

                    case MessageType.SERVER_RESPONSE:
                        ParseServerResponse(json.content);
                        break;

                    default:
                        LoggerHelper.Warn("WebSocket server send an invalid message type ({0}) with data: {1}", json.messageType, json.data);
                        break;
                }
            }
            catch (Exception)
            {

            }
        }

        private void ParseServerRequest(dynamic content)
        {
            RequestTypeServer type = content.type;
            dynamic data = content.data;

            LoggerHelper.Debug("ParseServerRequest - Request Type: {0} | Data: {1}", type, data);
            switch (type)
            {
                case RequestTypeServer.REQUEST_SHOW_MESSAGE:
                    string title = data.title;
                    string message = data.message;
                    WindowsAPI.ShowMessage(title, message);
                    break;

                case RequestTypeServer.REQUEST_DISPLAY_INFO:
                    SendResponse(ResponseTypeClient.RESPONSE_DISPLAY_INFO, WindowsAPI.GetDisplays());
                    break;

                case RequestTypeServer.REQUEST_DISPLAY_SCREENSHOT:
                    SendResponse(ResponseTypeClient.RESPONSE_DISPLAY_INFO, WindowsAPI.GetDisplays());
                    break;

                default:
                    LoggerHelper.Warn("ParseServerRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParseServerResponse(dynamic content)
        {
            throw new NotImplementedException();
        }

        private void SendMessage(MessageType messageType, dynamic contentType, dynamic data = null)
        {
            if (!IsConnected())
                return;

            dynamic msg = new ExpandoObject();
            msg.messageType = messageType;
            msg.content = new ExpandoObject();
            msg.content.type = contentType;
            msg.content.data = data;

            _client.Send(JsonConvert.SerializeObject(msg));
        }
    }
}
