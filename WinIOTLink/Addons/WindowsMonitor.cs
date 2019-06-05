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
            _cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuPerformanceCounter.NextValue();

            OnSessionChangeHandler += OnSessionChange;
            OnConfigReloadHandler += OnConfigReload;

            SetupTimers();
        }

        private void SetupTimers()
        {
            if (_config == null || !_config.Enabled)
            {
                LoggerHelper.Info(typeof(WindowsMonitor), "System monitor is disabled.");
                return;
            }

            if (_monitorTimer == null)
            {
                _monitorTimer = new Timer();
                _monitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimerElapsed);
            }

            _monitorTimer.Stop();
            _monitorTimer.Interval = _config.Interval * 1000;
            _monitorTimer.Start();

            LoggerHelper.Info(typeof(WindowsMonitor), "System monitor is set to an interval of {0} seconds.", _config.Interval);
        }

        private void OnConfigReload(object sender, EventArgs e)
        {
            SetupTimers();
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Info(typeof(WindowsMonitor), "OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            _manager.PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping

            LoggerHelper.Debug(typeof(WindowsMonitor), "System monitor running");

            MemoryInfo memoryInfo = WindowsHelper.GetMemoryInformation();
            string cpuUsage = Math.Round(_cpuPerformanceCounter.NextValue(), 0).ToString();
            string memoryUsage = memoryInfo.MemoryLoad.ToString();
            string memoryTotal = memoryInfo.TotalPhysical.ToString();
            string memoryAvailable = memoryInfo.AvailPhysical.ToString();
            string memoryUsed = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical).ToString();

            LoggerHelper.Debug(typeof(WindowsMonitor), "Processor Used: {0} %", cpuUsage);
            LoggerHelper.Debug(typeof(WindowsMonitor), "Memory Usage: {0} %", memoryUsage);
            LoggerHelper.Debug(typeof(WindowsMonitor), "Physical Available: {0} MB", memoryAvailable);
            LoggerHelper.Debug(typeof(WindowsMonitor), "Physical Used: {0} MB", memoryUsed);
            LoggerHelper.Debug(typeof(WindowsMonitor), "Physical Total: {0} MB", memoryTotal);

            _manager.PublishMessage(this, "Stats/CPU", cpuUsage);
            _manager.PublishMessage(this, "Stats/MemoryUsage", memoryUsage);
            _manager.PublishMessage(this, "Stats/MemoryAvailable", memoryAvailable);
            _manager.PublishMessage(this, "Stats/MemoryUsed", memoryUsed);
            _manager.PublishMessage(this, "Stats/MemoryTotal", memoryTotal);

            _monitorTimer.Start(); // After everything, start the timer again.
        }
    }
}
