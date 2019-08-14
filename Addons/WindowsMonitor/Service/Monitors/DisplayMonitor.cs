using IOTLinkAddon.Common;
using IOTLinkAPI.Platform;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class DisplayMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Display-Info";

        private static readonly Dictionary<string, AddonRequestType> AGENT_REQUESTS = new Dictionary<string, AddonRequestType>
        {
            { "Display-Info", AddonRequestType.REQUEST_DISPLAY_INFORMATION },
            { "Display-Screenshot", AddonRequestType.REQUEST_DISPLAY_SCREENSHOT }
        };

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override Dictionary<string, AddonRequestType> GetAgentRequests()
        {
            return AGENT_REQUESTS;
        }

        public override List<MonitorItem> OnAgentResponse(AddonRequestType type, dynamic data, string username)
        {
            switch (type)
            {
                case AddonRequestType.REQUEST_DISPLAY_INFORMATION:
                    return ParseDisplayInformation(data, username);

                case AddonRequestType.REQUEST_DISPLAY_SCREENSHOT:
                    return ParseDisplayScreenshot(data, username);

                default: break;
            }

            return null;
        }

        private List<MonitorItem> ParseDisplayInformation(dynamic data, string username)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            List<DisplayInfo> displayInfos = data.requestData.ToObject<List<DisplayInfo>>();
            for (var i = 0; i < displayInfos.Count; i++)
            {
                DisplayInfo displayInfo = displayInfos[i];

                string topic = string.Format("Stats/Display/{0}", i);

                result.Add(new MonitorItem
                {
                    ConfigKey = CONFIG_KEY,
                    Type = MonitorItemType.TYPE_RAW,
                    Topic = topic + "/ScreenWidth",
                    Value = displayInfo.ScreenWidth
                });

                result.Add(new MonitorItem
                {
                    ConfigKey = CONFIG_KEY,
                    Type = MonitorItemType.TYPE_RAW,
                    Topic = topic + "/ScreenHeight",
                    Value = displayInfo.ScreenHeight
                });
            }

            return result;
        }

        private List<MonitorItem> ParseDisplayScreenshot(dynamic data, string username)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW_BYTES,
                Topic = string.Format("Stats/Display/{0}/Screen", data.requestData.displayIndex),
                Value = (byte[])data.requestData.displayScreen
            });

            return result;
        }
    }
}
