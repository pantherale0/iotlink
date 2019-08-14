using IOTLinkAddon.Common;
using IOTLinkAPI.Configs;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    interface IMonitor
    {
        void Init();
        List<MonitorItem> GetMonitorItems(Configuration _config, int interval);
        Dictionary<string, AddonRequestType> GetAgentRequests();
        string GetConfigKey();
        List<MonitorItem> OnAgentResponse(AddonRequestType type, dynamic data, string username);
    }
}
