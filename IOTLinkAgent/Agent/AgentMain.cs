using IOTLinkAgent.Agent.WSClient;
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
            WebSocketClient.GetInstance().Init();
        }
    }
}
