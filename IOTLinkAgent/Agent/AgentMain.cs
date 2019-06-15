using IOTLinkAgent.Agent.WSClient;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System;
using System.Collections.Generic;

namespace IOTLinkAgent.Agent
{
    internal class AgentMain
    {
        private static AgentMain _instance;

        private DateTime _lastConfigChange;
        private string _webSocketUri;

        public static AgentMain GetInstance()
        {
            if (_instance == null)
                _instance = new AgentMain();

            return _instance;
        }

        private AgentMain()
        {
            ConfigHelper.SetEngineConfigReloadHandler(OnConfigChanged);
        }

        internal void Init(Dictionary<string, List<string>> commands)
        {
            if (commands.ContainsKey("agent"))
            {
                string uri = string.Concat(commands["agent"]);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    _webSocketUri = uri;

                    LoggerHelper.Debug("Initializing WebSocketClient - Server URI: {0}", _webSocketUri);
                    WebSocketClient.GetInstance().Init(_webSocketUri);

                    SetupAgent();
                }
            }
        }

        private void OnConfigChanged(object sender, ConfigReloadEventArgs e)
        {
            if (_lastConfigChange == null || _lastConfigChange.AddSeconds(1) <= DateTime.Now)
            {
                LoggerHelper.Info("Changes to configuration.yaml detected. Reloading.");

                SetupAgent();
                AgentAddonManager.GetInstance().Raise_OnConfigReloadHandler(this, e);

                _lastConfigChange = DateTime.Now;
            }
        }

        private void SetupAgent()
        {
            LoggerHelper.Debug("Initializing AgentAddonManager");
            AgentAddonManager.GetInstance().LoadAddons();
        }
    }
}
