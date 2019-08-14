using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class MemoryMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Memory";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            MemoryInfo memoryInfo = PlatformHelper.GetMemoryInformation();

            // Memory Usage (%)
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Memory/Usage",
                Value = memoryInfo.MemoryLoad
            });

            // Memory Available
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_MEMORY_SIZE,
                Topic = "Stats/Memory/Available",
                Value = memoryInfo.AvailPhysical
            });

            // Memory Used
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_MEMORY_SIZE,
                Topic = "Stats/Memory/Used",
                Value = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical)
            });

            // Memory Total
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_MEMORY_SIZE,
                Topic = "Stats/Memory/Total",
                Value = memoryInfo.TotalPhysical
            });

            return result;
        }
    }
}
