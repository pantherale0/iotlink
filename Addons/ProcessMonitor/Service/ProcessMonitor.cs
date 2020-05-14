using IOTLinkAddon.Common.Configs;
using IOTLinkAddon.Common.Processes;
using System;

namespace IOTLinkAddon.Service
{
    public class ProcessMonitor
    {
        public string Name { get; set; }
        public MonitorConfig Config { get; set; }
        public ProcessInformation Process { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
