using System;
using System.Diagnostics;
using System.Timers;
using WinIOTLink.API;
using WinIOTLink.Engine.System;
using WinIOTLink.Helpers;
using static WinIOTLink.Configs.ApplicationConfig;
using static WinIOTLink.Platform.Windows.WindowsAPI;

namespace WinIOTLink.Addons
{
    internal class WindowsMonitor : AddonScript
    {
        private Timer _monitorTimer;
        private MonitorConfig _config;
        private PerformanceCounter _cpuPerformanceCounter;

        public override void Init()
        {
            base.Init();

            _config = ConfigHelper.GetApplicationConfig().Monitor;
            if (_config == null || !_config.Enabled)
            {
                LoggerHelper.Info(typeof(WindowsMonitor), "System monitor is disabled.");
                return;
            }

            _cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuPerformanceCounter.NextValue();

            int seconds = _config.Interval;
            _monitorTimer = new Timer();
            _monitorTimer.Interval = seconds * 1000;
            _monitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimerElapsed);
            _monitorTimer.Enabled = true;

            LoggerHelper.Info(typeof(WindowsMonitor), "System monitor is set to an interval of {0} seconds.", seconds);

            OnSessionChangeHandler += OnSessionChange;
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Info(typeof(WindowsMonitor), "OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            _manager.PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Debug(typeof(WindowsMonitor), "System monitor running");

            LoggerHelper.Debug(typeof(WindowsMonitor), string.Format("CPU Utilization: {0}", _cpuPerformanceCounter.NextValue()));

            MemoryInfo memoryInfo = WindowsHelper.GetMemoryInformation();
            LoggerHelper.Debug(typeof(WindowsMonitor), "Physical Total: {0}", memoryInfo.TotalPhysical);
            LoggerHelper.Debug(typeof(WindowsMonitor), "Physical Available: {0}", memoryInfo.AvailPhysical);
        }
    }
}
