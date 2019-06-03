using System;
using System.Timers;
using WinIOTLink.Helpers;
using static WinIOTLink.Configs.ApplicationConfig;
using static WinIOTLink.Helpers.LoggerHelper;

namespace WinIOTLink
{
    class SystemMonitor
    {
        Timer MonitorTimer = new Timer();

        public void SetupMonitor(MonitorConfig Monitor)
        {
            MonitorTimer.Interval = Monitor.Interval * 1000;
            MonitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimer);
            MonitorTimer.Enabled = true;

            LoggerHelper.WriteToFile("SetupMonitor", String.Format("System monitor is set to an interval of {0} seconds.", Monitor.Interval), LogLevel.INFO);
        }

        private void OnMonitorTimer(object source, ElapsedEventArgs e)
        {
            LoggerHelper.WriteToFile("OnMonitorTimer", "System monitor running", LogLevel.DEBUG);
        }
    }
}
