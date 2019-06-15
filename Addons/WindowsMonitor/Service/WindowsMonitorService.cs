using IOTLinkAddon.Common;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Timers;
using System.Windows.Forms;

namespace IOTLinkAddon.Service
{
    public class WindowsMonitorService : ServiceAddon
    {
        private System.Timers.Timer _monitorTimer;
        private MonitorConfig _config;
        private PerformanceCounter _cpuPerformanceCounter;
        private Dictionary<string, string> _cache = new Dictionary<string, string>();
        private string _configPath;

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);


            _configPath = Path.Combine(this._currentPath, "config.yaml");
            ConfigHelper.SetReloadHandler<MonitorConfig>(_configPath, OnConfigReload);

            _config = ConfigHelper.GetConfiguration<MonitorConfig>(_configPath);

            _cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuPerformanceCounter.NextValue();

            OnSessionChangeHandler += OnSessionChange;
            OnConfigReloadHandler += OnConfigReload;
            OnAgentResponseHandler += OnAgentResponse;

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

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            if (e.ConfigType != ConfigType.CONFIGURATION_ADDON)
                return;

            _config = ConfigHelper.GetConfiguration<MonitorConfig>(_configPath);
            SetupTimers();
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Debug("OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            GetManager().PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping
            LoggerHelper.Trace("OnMonitorTimerElapsed: Started");


            SendCPUInfo();
            SendMemoryInfo();
            SendPowerInfo();
            SendHardDriveInfo();
            RequestAgentIdleTime();
            RequestAgentDisplayInfo();
            RequestAgentDisplayScreenshot();

            LoggerHelper.Trace("OnMonitorTimerElapsed: Completed");
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

        private void RequestAgentIdleTime()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_IDLE_TIME;

            GetManager().SendAgentRequest(this, addonData);
        }

        private void RequestAgentDisplayInfo()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_INFORMATION;

            GetManager().SendAgentRequest(this, addonData);
        }

        private void RequestAgentDisplayScreenshot()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_SCREENSHOT;

            GetManager().SendAgentRequest(this, addonData);
        }

        private void OnAgentResponse(object sender, AgentAddonResponseEventArgs e)
        {
            AddonRequestType requestType = e.Data.requestType;
            switch (requestType)
            {
                case AddonRequestType.REQUEST_IDLE_TIME:
                    ParseIdleTime(e.Data, e.Username);
                    break;

                case AddonRequestType.REQUEST_DISPLAY_INFORMATION:
                    ParseDisplayInfo(e.Data, e.Username);
                    break;

                case AddonRequestType.REQUEST_DISPLAY_SCREENSHOT:
                    ParseDisplayScreenshot(e.Data, e.Username);
                    break;

                default: break;
            }
        }

        private void ParseIdleTime(dynamic data, string username)
        {
            uint idleTime = (uint)data.requestData;

            SendMonitorValue("Stats/IdleTime/" + username, idleTime.ToString());
        }

        private void ParseDisplayInfo(dynamic data, string username)
        {
            List<DisplayInfo> displayInfos = data.requestData.ToObject<List<DisplayInfo>>();
            for (var i = 0; i < displayInfos.Count; i++)
            {
                DisplayInfo displayInfo = displayInfos[i];

                string topic = string.Format("Stats/Display/{0}", i);
                SendMonitorValue(topic + "/ScreenWidth", displayInfo.ScreenWidth.ToString());
                SendMonitorValue(topic + "/ScreenHeight", displayInfo.ScreenHeight.ToString());
            }
        }

        private void ParseDisplayScreenshot(dynamic data, string username)
        {
            int displayIndex = data.requestData.displayIndex;
            byte[] displayScreen = data.requestData.displayScreen;
            string topic = string.Format("Stats/Display/{0}/Screen", displayIndex);

            GetManager().PublishMessage(this, topic, displayScreen);
        }

        private void SendMonitorValue(string topic, string value)
        {
            if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                return;

            _cache[topic] = value;
            GetManager().PublishMessage(this, topic, value);
        }
    }
}
