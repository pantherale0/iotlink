using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IOTLinkAddon.Service.Monitors
{
    class UptimeMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Uptime";

        private PerformanceCounter _uptimePerformanceCounter = new PerformanceCounter("System", "System Up Time");

        public override void Init()
        {
            _uptimePerformanceCounter.NextValue();
        }

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            DateTimeOffset lastBootUpTime = PlatformHelper.LastBootUpTime();

            // Boot Time
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_DATETIME,
                Topic = "Stats/System/BootTime",
                Value = PlatformHelper.LastBootUpTime(),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "BootTime",
                    Name = "System Boot Time",
                    Unit = "s",
                    Icon = "mdi:timer"
                }
            });

            // Uptime
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = config.GetValue("inSeconds", false) ? MonitorItemType.TYPE_RAW : MonitorItemType.TYPE_UPTIME,
                Topic = "Stats/System/Uptime",
                Value = Math.Round(_uptimePerformanceCounter.NextValue(), 0),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Uptime",
                    Name = "System Uptime",
                    Unit = "s",
                    Icon = "mdi:timer"
                }
            });

            return result;
        }
    }
}
