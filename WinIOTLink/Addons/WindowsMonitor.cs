using System;
using System.Timers;
using WinIOTLink.API;
using WinIOTLink.Helpers;

namespace WinIOTLink.Addons
{
    internal class WindowsMonitor : AddonScript
    {
        private Timer _monitorTimer;

        public override void Init()
        {
            base.Init();

            int seconds = ConfigHelper.GetApplicationConfig().Monitor.Interval;
            _monitorTimer = new Timer();
            _monitorTimer.Interval = seconds * 1000;
            _monitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimerElapsed);
            _monitorTimer.Enabled = true;

            LoggerHelper.Info("SetupMonitor", string.Format("System monitor is set to an interval of {0} seconds.", seconds));
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Info("OnMonitorTimer", "System monitor running");
        }
    }
}
