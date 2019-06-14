using IOTLinkAPI.Addons;
using IOTLinkAPI.Platform.Events;

namespace IOTLinkAddon.Agent
{
    public class WindowsMonitorAgent : AgentAddon
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            OnConfigReloadHandler += OnConfigReload;
            OnAgentRequestHandler += OnAgentRequest;
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void OnAgentRequest(object sender, AgentAddonRequestEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
