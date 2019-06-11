using IOTLink.API;
using IOTLink.Engine.System;
using IOTLink.Helpers;
using IOTLink.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows.Forms;
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
        private Dictionary<string, string> _cache = new Dictionary<string, string>();

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


            SendCPUInfo();
            SendMemoryInfo();
            SendPowerInfo();
            SendHardDriveInfo();

            LoggerHelper.Debug("OnMonitorTimerElapsed: Completed");
            _monitorTimer.Start(); // After everything, start the timer again.
        }

        private void SendCPUInfo()
        {
            string cpuUsage = Math.Round(_cpuPerformanceCounter.NextValue(), 0).ToString();
            SendMonitorValue("Stats/CPU", cpuUsage);
        }

        private void SendMemoryInfo()
        {
            MemoryInfo memoryInfo = PlatformHelper.GetMemoryInformation();
            string memoryUsage = memoryInfo.MemoryLoad.ToString();
            string memoryTotal = memoryInfo.TotalPhysical.ToString();
            string memoryAvailable = memoryInfo.AvailPhysical.ToString();
            string memoryUsed = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical).ToString();

            SendMonitorValue("Stats/Memory/Usage", memoryUsage);
            SendMonitorValue("Stats/Memory/Available", memoryAvailable);
            SendMonitorValue("Stats/Memory/Used", memoryUsed);
            SendMonitorValue("Stats/Memory/Total", memoryTotal);
        }

        private void SendPowerInfo()
        {
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            string powerLineStatus = powerStatus.PowerLineStatus.ToString();
            string batteryChargeStatus = powerStatus.BatteryChargeStatus.ToString();
            string batteryFullLifetime = powerStatus.BatteryFullLifetime.ToString();
            string batteryLifePercent = (powerStatus.BatteryLifePercent * 100).ToString();
            string batteryLifeRemaining = powerStatus.BatteryLifeRemaining.ToString();

            SendMonitorValue("Stats/Power/Status", powerLineStatus);
            SendMonitorValue("Stats/Battery/Status", batteryChargeStatus);
            SendMonitorValue("Stats/Battery/FullLifetime", batteryFullLifetime);
            SendMonitorValue("Stats/Battery/RemainingTime", batteryLifeRemaining);
            SendMonitorValue("Stats/Battery/RemainingPercent", batteryLifePercent);
        }

        private void SendHardDriveInfo()
        {
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType != DriveType.Fixed)
                    continue;

                SendHardDriveInfo(driveInfo);
            }
        }

        private void SendHardDriveInfo(DriveInfo driveInfo)
        {
            string drive = driveInfo.Name.Remove(1, 2);
            string topic = string.Format("Stats/HardDrive/{0}", drive);

            SendMonitorValue(topic + "/TotalSize", (driveInfo.TotalSize / (1024 * 1024)).ToString());
            SendMonitorValue(topic + "/AvailableFreeSpace", (driveInfo.AvailableFreeSpace / (1024 * 1024)).ToString());
            SendMonitorValue(topic + "/TotalFreeSpace", (driveInfo.TotalFreeSpace / (1024 * 1024)).ToString());
            SendMonitorValue(topic + "/DriveFormat", driveInfo.DriveFormat);
            SendMonitorValue(topic + "/VolumeLabel", driveInfo.VolumeLabel);
        }

        private void SendMonitorValue(string topic, string value)
        {
            if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                return;

            _cache[topic] = value;
            _manager.PublishMessage(this, topic, value);
        }
    }
}
