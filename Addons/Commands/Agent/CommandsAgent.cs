using IOTLinkAddon.Common;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System.Dynamic;

namespace IOTLinkAddon.Agent
{
    public class CommandsAgent : AgentAddon
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            OnConfigReloadHandler += OnConfigReload;
            OnAgentRequestHandler += OnAgentRequest;
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            LoggerHelper.Verbose("WindowsMonitorAgent::OnConfigReload");
        }

        private void OnAgentRequest(object sender, AgentAddonRequestEventArgs e)
        {
            LoggerHelper.Verbose("WindowsMonitorAgent::OnAgentRequest");

            AddonRequestType requestType = e.Data.requestType;

            dynamic addonData = new ExpandoObject();
            addonData.requestType = requestType;
            switch (requestType)
            {
                case AddonRequestType.REQUEST_DISPLAY_TURN_ON:
                    DisplayTurnOn();
                    break;

                case AddonRequestType.REQUEST_DISPLAY_TURN_OFF:
                    DisplayTurnOff();
                    break;

                default: break;
            }
        }

        private void DisplayTurnOn()
        {
            PlatformHelper.TurnOnDisplays();
        }

        private void DisplayTurnOff()
        {
            PlatformHelper.TurnOffDisplays();
        }
    }
}
