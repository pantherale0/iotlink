using System;
using System.Diagnostics;
using System.Timers;
using IOTLink.API;
using IOTLink.Engine.System;
using IOTLink.Helpers;
using static IOTLink.Configs.ApplicationConfig;
using static IOTLink.Platform.Windows.WindowsAPI;

namespace IOTLink.Addons
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
                LoggerHelper.Info("System monitor is disabled.");
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

            LoggerHelper.Info("System monitor is set to an interval of {0} seconds.", _config.Interval);
        }

        private void OnConfigReload(object sender, EventArgs e)
        {
            SetupTimers();
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Info("OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            _manager.PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping

            LoggerHelper.Debug("System monitor running");

            MemoryInfo memoryInfo = WindowsHelper.GetMemoryInformation();
            string cpuUsage = Math.Round(_cpuPerformanceCounter.NextValue(), 0).ToString();
            string memoryUsage = memoryInfo.MemoryLoad.ToString();
            string memoryTotal = memoryInfo.TotalPhysical.ToString();
            string memoryAvailable = memoryInfo.AvailPhysical.ToString();
            string memoryUsed = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical).ToString();

            LoggerHelper.Debug("Processor Used: {0} %", cpuUsage);
            LoggerHelper.Debug("Memory Usage: {0} %", memoryUsage);
            LoggerHelper.Debug("Physical Available: {0} MB", memoryAvailable);
            LoggerHelper.Debug("Physical Used: {0} MB", memoryUsed);
            LoggerHelper.Debug("Physical Total: {0} MB", memoryTotal);

            _manager.PublishMessage(this, "Stats/CPU", cpuUsage);
            _manager.PublishMessage(this, "Stats/MemoryUsage", memoryUsage);
            _manager.PublishMessage(this, "Stats/MemoryAvailable", memoryAvailable);
            _manager.PublishMessage(this, "Stats/MemoryUsed", memoryUsed);
            _manager.PublishMessage(this, "Stats/MemoryTotal", memoryTotal);

            _monitorTimer.Start(); // After everything, start the timer again.
        }
    }
}
