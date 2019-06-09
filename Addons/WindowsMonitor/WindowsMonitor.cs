using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows.Forms;
using IOTLink.API;
using IOTLink.Engine.System;
using IOTLink.Helpers;
using IOTLink.Platform;
using YamlDotNet.Serialization;

namespace IOTLinkAddon
{
    public class MonitorConfig
    {
        [YamlMember(Alias = "enabled")]
        public bool Enabled { get; set; }

        [YamlMember(Alias = "interval")]
        public int Interval { get; set; }
    }

    public class WindowsMonitor : AddonScript
    {
        private System.Timers.Timer _monitorTimer;
        private MonitorConfig _config;
        private PerformanceCounter _cpuPerformanceCounter;

        public override void Init()
        {
            base.Init();

            _config = ConfigHelper.GetConfig<MonitorConfig>(Path.Combine(this._currentPath, "config.yaml"));
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
                _monitorTimer = new System.Timers.Timer();
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
            LoggerHelper.Debug("OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            _manager.PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping
            LoggerHelper.Debug("OnMonitorTimerElapsed: Started");


            string cpuUsage = Math.Round(_cpuPerformanceCounter.NextValue(), 0).ToString();

            MemoryInfo memoryInfo = PlatformHelper.GetMemoryInformation();
            string memoryUsage = memoryInfo.MemoryLoad.ToString();
            string memoryTotal = memoryInfo.TotalPhysical.ToString();
            string memoryAvailable = memoryInfo.AvailPhysical.ToString();
            string memoryUsed = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical).ToString();

            PowerStatus powerStatus = SystemInformation.PowerStatus;
            string powerLineStatus = powerStatus.PowerLineStatus.ToString();
            string batteryChargeStatus = powerStatus.BatteryChargeStatus.ToString();
            string batteryFullLifetime = powerStatus.BatteryFullLifetime.ToString();
            string batteryLifePercent = (powerStatus.BatteryLifePercent * 100).ToString();
            string batteryLifeRemaining = powerStatus.BatteryLifeRemaining.ToString();

            _manager.PublishMessage(this, "Stats/CPU", cpuUsage);

            _manager.PublishMessage(this, "Stats/Memory/Usage", memoryUsage);
            _manager.PublishMessage(this, "Stats/Memory/Available", memoryAvailable);
            _manager.PublishMessage(this, "Stats/Memory/Used", memoryUsed);
            _manager.PublishMessage(this, "Stats/Memory/Total", memoryTotal);

            _manager.PublishMessage(this, "Stats/Power/Status", powerLineStatus);

            _manager.PublishMessage(this, "Stats/Battery/Status", batteryChargeStatus);
            _manager.PublishMessage(this, "Stats/Battery/FullLifetime", batteryFullLifetime);
            _manager.PublishMessage(this, "Stats/Battery/RemainingTime", batteryLifeRemaining);
            _manager.PublishMessage(this, "Stats/Battery/RemainingPercent", batteryLifePercent);

            LoggerHelper.Debug("OnMonitorTimerElapsed: Completed");
            _monitorTimer.Start(); // After everything, start the timer again.
        }
    }
}
