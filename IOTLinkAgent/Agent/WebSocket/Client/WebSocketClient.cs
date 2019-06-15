using IOTLink.Platform.WebSocket;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkAPI.Platform.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        internal void Init(string uri)
        {
            if (_client != null)
                Disconnect();

            _client = new WebSocket(uri);
            _client.OnOpen += OnOpen;
            _client.OnClose += OnClose;
            _client.OnError += OnError;
            _client.OnMessage += OnMessageReceived;
            _client.Connect();
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

        private void OnOpen(object sender, EventArgs e)
        {
            LoggerHelper.Debug("WebSocketClient - Connection Opened.");

            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add("username", Environment.UserName);

            SendRequest(RequestTypeClient.REQUEST_CONNECTED, keyValuePairs);
        }

        private void OnClose(object sender, CloseEventArgs arg)
        {
            LoggerHelper.Debug("WebSocketClient - Connection Closed (Clean: {0}).", arg.WasClean);
        }

        private void OnError(object sender, ErrorEventArgs arg)
        {
            LoggerHelper.Error("WebSocketClient - Error: {0}", arg.Message);
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.RawData == null || e.RawData.Length == 0 || !e.IsText || string.IsNullOrWhiteSpace(e.Data))
            {
                LoggerHelper.Trace("OnMessageReceived: Invalid message content [1].");
                return;
            }

            string data = e.Data;
            LoggerHelper.Trace("OnMessageReceived - Data: {0}", data);

            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(data);
                if (json == null || json.messageType == null || json.content == null)
                {
                    LoggerHelper.Trace("OnMessageReceived: Invalid message content [2].");
                    return;
                }

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
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnMessage - Exception: {0}", ex.ToString());
            }
        }

        private void ParseServerResponse(dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseServerResponse: Invalid message content.");
                return;
            }

            LoggerHelper.Trace("ParseServerResponse: Content: {0]", content);
        }

        private void ParseServerRequest(dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseServerRequest: Invalid message content.");
                return;
            }

            RequestTypeServer type = content.type;
            dynamic data = content.data;

            LoggerHelper.Trace("ParseServerRequest: Request Type: {0} | Data: {1}", type, data);
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

            LoggerHelper.Trace("ParseShowMessage: Title: {0} Message: {1}", title, message);
            WindowsAPI.ShowMessage(title, message);
        }

        private void ParseShowNotification(dynamic data)
        {
            LoggerHelper.Trace("ParseShowNotification: Data: {0]", data);
        }

        private void ParseAddonRequest(dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            LoggerHelper.Trace("ParseAddonRequest: AddonId: {0} AddonData: {1}", addonId, addonData);
            AgentAddonManager.GetInstance().Raise_OnAgentRequest(addonId, addonData);
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
