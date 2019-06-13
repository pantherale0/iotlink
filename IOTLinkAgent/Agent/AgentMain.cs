using IOTLinkAgent.Agent.WSClient;
using IOTLinkAPI.Helpers;
using System.Collections.Generic;

namespace IOTLinkAgent.Agent
{
    internal class MainAgent
    {
        private static MainAgent _instance;

        public static MainAgent GetInstance()
        {
            if (_instance == null)
                _instance = new MainAgent();

            return _instance;
        }

        private MainAgent()
        {
        }

        internal void Init(Dictionary<string, List<string>> commands)
        {
            if (commands.ContainsKey("agent"))
            {
                string uri = string.Concat(commands["agent"]);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    LoggerHelper.Debug("Initializing WebSocketClient - Server URI: {0}", uri);
                    WebSocketClient.GetInstance().Init(uri);
                }
            }
        }
    }
}
