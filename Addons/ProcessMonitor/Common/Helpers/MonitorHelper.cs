using IOTLinkAddon.Common.Configs;
using IOTLinkAddon.Common.Processes;
using IOTLinkAddon.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IOTLinkAddon.Common.Helpers
{
    public static class MonitorHelper
    {
        public static List<ProcessMonitor> GetProcessMonitorsByName(List<ProcessMonitor> monitors, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return monitors.Where(x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0).ToList();
        }

        public static ProcessMonitor GetProcessMonitorByName(List<ProcessMonitor> monitors, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return monitors.FirstOrDefault(x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static List<ProcessMonitor> GetProcessMonitorsByProcessName(List<ProcessMonitor> monitors, string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            processName = ProcessHelper.CleanProcessName(processName);
            return monitors.Where(x => x.Config.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public static ProcessMonitor GetProcessMonitorByProcessName(List<ProcessMonitor> monitors, string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            processName = ProcessHelper.CleanProcessName(processName);
            return monitors.FirstOrDefault(x => x.Config.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase));
        }

        public static ProcessMonitor GetProcessMonitorByProcessId(List<ProcessMonitor> monitors, int processId)
        {
            return monitors.FirstOrDefault(x => x.Process?.Id == processId);
        }

        public static List<ProcessMonitor> GetProcessMonitorsByProcessInfo(List<ProcessMonitor> monitors, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return new List<ProcessMonitor>();

            List<ProcessMonitor> processMonitors = GetProcessMonitorsByProcessName(monitors, processInfo.ProcessName);
            return processMonitors.Where(pm => CheckMonitorFilters(pm, processInfo)).ToList();
        }

        public static bool HasConditions(ProcessMonitor monitor)
        {
            return (monitor.Config.ProcessWindows.Count != 0 || monitor.Config.ProcessClassNames.Count != 0);
        }

        public static bool CheckMonitorFilters(ProcessMonitor monitor, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return false;

            MonitorConfig config = monitor.Config;
            if (!config.ProcessNames.Contains(processInfo.ProcessName, StringComparer.OrdinalIgnoreCase))
                return false;

            if (!HasConditions(monitor))
                return true;

            var processWindows = config.ProcessWindows;
            var processClasses = config.ProcessClassNames;

            var compareType = config.Monitoring.CompareType;
            var windowMatches = 0;
            var classMatches = 0;

            if (processInfo.Windows != null)
            {
                foreach (string windowName in processWindows)
                {
                    bool hasFound = processInfo.Windows.Any(w => Regex.IsMatch(w, windowName));
                    if (hasFound)
                        windowMatches++;
                }
            }

            if (processInfo.ClassNames != null)
            {
                foreach (string className in processClasses)
                {
                    bool hasFound = processInfo.ClassNames.Any(w => Regex.IsMatch(w, className));
                    if (hasFound)
                        classMatches++;
                }
            }

            switch (compareType)
            {
                default:
                    return (processWindows.Count == windowMatches) && (processClasses.Count == classMatches);

                case 1:
                    return (processWindows.Count == windowMatches) || (processClasses.Count == classMatches);

                case 2:
                    return (processWindows.Count == windowMatches) && (classMatches > 0);

                case 3:
                    return (processClasses.Count == classMatches) && (windowMatches > 0);

                case 4:
                    return (windowMatches > 0) || (classMatches > 0);
            }
        }
    }
}
