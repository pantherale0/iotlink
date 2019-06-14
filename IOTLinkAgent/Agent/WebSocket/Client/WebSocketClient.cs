using IOTLink.Platform.WebSocket;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkAPI.Platform.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
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

        internal void Init(string uri)
        {
            if (_client != null)
                Disconnect();

            _client = new WebSocket(uri, onOpen: OnOpen, onClose: OnClose, onError: OnError, onMessage: OnMessageReceived);
            _client.Connect().ConfigureAwait(false);
        }

        internal void Disconnect()
        {
            if (_client == null)
                return;

            if (_client.IsAlive().Result)
                _client.Close();

            _client = null;
        }

        internal bool IsConnected()
        {
            bool isAlive = _client.IsAlive().Result;
            return _client != null && isAlive;
        }

        internal void SendRequest(RequestTypeClient type, dynamic data = null)
        {
            SendMessage(MessageType.CLIENT_REQUEST, type, data);
        }

        internal void SendResponse(ResponseTypeClient type, dynamic data = null)
        {
            SendMessage(MessageType.CLIENT_RESPONSE, type, data);
        }

        private async Task OnOpen()
        {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add("username", Environment.UserName);

            SendRequest(RequestTypeClient.REQUEST_CONNECTED, keyValuePairs);
        }

        private async Task OnClose(CloseEventArgs arg)
        {
            LoggerHelper.Debug("WebSocketClient - Connection Closed (Clean: {0}).", arg.WasClean);
        }

        private async Task OnError(ErrorEventArgs arg)
        {
            LoggerHelper.Error("WebSocketClient - Error: {0}", arg.Message);
        }

        private async Task OnMessageReceived(MessageEventArgs e)
        {
            if (e.Data == null || e.Data.Length == 0)
                return;

            if (e.Opcode != Opcode.Text)
                return;

            string data = e.Text.ReadToEnd();
            if (string.IsNullOrWhiteSpace(data))
                return;

            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(data);
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

        private void ParseServerResponse(dynamic content)
        {
            LoggerHelper.Trace("ParseShowNotification - Content: {0]", content);
        }

        private void ParseServerRequest(dynamic content)
        {
            RequestTypeServer type = content.type;
            dynamic data = content.data;

            LoggerHelper.Debug("ParseServerRequest - Request Type: {0} | Data: {1}", type, data);
            switch (type)
            {
                case RequestTypeServer.REQUEST_SHOW_MESSAGE:
                    ParseShowMessage(data);
                    break;

                case RequestTypeServer.REQUEST_SHOW_NOTIFICATION:
                    ParseShowNotification(data);
                    break;

                case RequestTypeServer.REQUEST_ADDON:
                    ParseAddonRequest(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseServerRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParseShowMessage(dynamic data)
        {
            string title = data.title;
            string message = data.message;

            LoggerHelper.Trace("ParseShowMessage - Title: {0} Message: {1}", title, message);
            WindowsAPI.ShowMessage(title, message);
        }

        private void ParseShowNotification(dynamic data)
        {
            LoggerHelper.Trace("ParseShowNotification - Data: {0]", data);
        }

        private void ParseAddonRequest(dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            LoggerHelper.Trace("ParseAddonRequest - AddonId: {0} AddonData: {1}", addonId, addonData);
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
