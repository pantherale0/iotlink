using IOTLinkAPI.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IOTLinkAddon.Service.Monitors
{
    class CPUMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "CPU";

        private PerformanceCounter _cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        public override void Init()
        {
            _cpuPerformanceCounter.NextValue();
        }

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration _config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            // CPU Usage
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/CPU/Usage",
                Value = Math.Round(_cpuPerformanceCounter.NextValue(), 0)
            });

            return result;
        }
    }
}
