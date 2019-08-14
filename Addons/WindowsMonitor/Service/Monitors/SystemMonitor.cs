using IOTLinkAddon.Common;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class SystemMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "SystemInfo";

        private static readonly Dictionary<string, AddonRequestType> AGENT_REQUESTS = new Dictionary<string, AddonRequestType>
        {
            { "IdleTime", AddonRequestType.REQUEST_IDLE_TIME }
        };

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override Dictionary<string, AddonRequestType> GetAgentRequests()
        {
            return AGENT_REQUESTS;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration _config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/System/CurrentUser",
                Value = PlatformHelper.GetCurrentUsername()
            });

            return result;
        }

        public override List<MonitorItem> OnAgentResponse(AddonRequestType type, dynamic data, string username)
        {
            switch (type)
            {
                case AddonRequestType.REQUEST_IDLE_TIME:
                    return ParseIdleTimeInformation(data, username);

                default: break;
            }

            return null;
        }

        private List<MonitorItem> ParseIdleTimeInformation(dynamic data, string username)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/System/IdleTime",
                Value = data.requestData
            });

            return result;
        }
    }
}
