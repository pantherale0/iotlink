using System;
using System.Timers;
using WinIOTLink.API;
using WinIOTLink.Engine.System;
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

            LoggerHelper.Info("WindowsMonitor", string.Format("System monitor is set to an interval of {0} seconds.", seconds));

            OnSessionChangeHandler += OnSessionChange;
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Info("WindowsMonitor", string.Format("OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username));

            _manager.PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Info("WindowsMonitor", "System monitor running");
        }
    }
}
