using IOTLinkAddon.Common;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System.Dynamic;

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
            LoggerHelper.Debug("WindowsMonitorAgent::OnConfigReload - {0}", e);
        }

        private void OnAgentRequest(object sender, AgentAddonRequestEventArgs e)
        {
            LoggerHelper.Debug("WindowsMonitorAgent::OnAgentRequest - {0}", e);

            AddonRequestType requestType = e.Data.requestType;

            dynamic addonData = new ExpandoObject();
            addonData.requestType = requestType;
            switch (requestType)
            {
                case AddonRequestType.REQUEST_IDLE_TIME:
                    SendIdleTime();
                    break;

                case AddonRequestType.REQUEST_DISPLAY_INFORMATION:
                    SendDisplayInfo();
                    break;

                case AddonRequestType.REQUEST_DISPLAY_SCREENSHOT:
                    break;

                default: break;
            }
        }

        private void SendIdleTime()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_IDLE_TIME;
            addonData.requestData = PlatformHelper.GetIdleTime();
            GetManager().SendAgentResponse(this, addonData);
        }

        private void SendDisplayInfo()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_INFORMATION;
            addonData.requestData = PlatformHelper.GetDisplays();
            GetManager().SendAgentResponse(this, addonData);
        }
    }
}
