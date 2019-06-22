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
        private uint _monitorCounter = 0;

        private string _configPath;
        private WindowsMonitorConfig _config;

        private PerformanceCounter _cpuPerformanceCounter;
        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        private string _currentUser = "SYSTEM";

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);


            _configPath = Path.Combine(this._currentPath, "config.yaml");
            ConfigHelper.SetReloadHandler<WindowsMonitorConfig>(_configPath, OnConfigReload);

            _config = ConfigHelper.GetConfiguration<WindowsMonitorConfig>(_configPath);

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
            _monitorTimer.Interval = 1000;
            _monitorTimer.Start();

            LoggerHelper.Info("System monitor is activated.");
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            if (e.ConfigType != ConfigType.CONFIGURATION_ADDON)
                return;

            _config = ConfigHelper.GetConfiguration<WindowsMonitorConfig>(_configPath);
            SetupTimers();
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Verbose("OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            GetManager().PublishMessage(this, e.Reason.ToString(), e.Username);

            if (e.Reason == System.ServiceProcess.SessionChangeReason.SessionLogon || e.Reason == System.ServiceProcess.SessionChangeReason.SessionUnlock)
            {
                _currentUser = e.Username;
                SendCurrentUserInfo();
            }

            if (e.Reason == System.ServiceProcess.SessionChangeReason.SessionLogoff || e.Reason == System.ServiceProcess.SessionChangeReason.SessionLock)
            {
                _currentUser = "SYSTEM";
                SendCurrentUserInfo();
            }
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping
            LoggerHelper.Debug("OnMonitorTimerElapsed: Started");

            SendCPUInfo();
            SendMemoryInfo();
            SendPowerInfo();
            SendHardDriveInfo();
            SendCurrentUserInfo();
            RequestAgentIdleTime();
            RequestAgentDisplayInfo();
            RequestAgentDisplayScreenshot();

            if (_monitorCounter++ == uint.MaxValue)
                _monitorCounter = 0;

            LoggerHelper.Debug("OnMonitorTimerElapsed: Completed");
            _monitorTimer.Start(); // After everything, start the timer again.
        }

        private void SendCPUInfo()
        {
            const string configKey = "CPU";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);
            string cpuUsage = Math.Round(_cpuPerformanceCounter.NextValue(), 0).ToString();
            SendMonitorValue("Stats/CPU", cpuUsage, configKey);
        }

        private void SendMemoryInfo()
        {
            const string configKey = "Memory";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

            MemoryInfo memoryInfo = PlatformHelper.GetMemoryInformation();
            string memoryUsage = memoryInfo.MemoryLoad.ToString();
            string memoryTotal = memoryInfo.TotalPhysical.ToString();
            string memoryAvailable = memoryInfo.AvailPhysical.ToString();
            string memoryUsed = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical).ToString();

            SendMonitorValue("Stats/Memory/Usage", memoryUsage, configKey);
            SendMonitorValue("Stats/Memory/Available", memoryAvailable, configKey);
            SendMonitorValue("Stats/Memory/Used", memoryUsed, configKey);
            SendMonitorValue("Stats/Memory/Total", memoryTotal, configKey);
        }

        private void SendPowerInfo()
        {
            const string configKey = "Power";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

            PowerStatus powerStatus = SystemInformation.PowerStatus;
            string powerLineStatus = powerStatus.PowerLineStatus.ToString();
            string batteryChargeStatus = powerStatus.BatteryChargeStatus.ToString();
            string batteryFullLifetime = powerStatus.BatteryFullLifetime.ToString();
            string batteryLifePercent = (powerStatus.BatteryLifePercent * 100).ToString();
            string batteryLifeRemaining = powerStatus.BatteryLifeRemaining.ToString();

            SendMonitorValue("Stats/Power/Status", powerLineStatus, configKey);
            SendMonitorValue("Stats/Battery/Status", batteryChargeStatus, configKey);
            SendMonitorValue("Stats/Battery/FullLifetime", batteryFullLifetime, configKey);
            SendMonitorValue("Stats/Battery/RemainingTime", batteryLifeRemaining, configKey);
            SendMonitorValue("Stats/Battery/RemainingPercent", batteryLifePercent, configKey);
        }

        private void SendHardDriveInfo()
        {
            const string configKey = "HardDrive";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType != DriveType.Fixed)
                    continue;

                string drive = driveInfo.Name.Remove(1, 2);
                string topic = string.Format("Stats/HardDrive/{0}", drive);

                long usedSpace = driveInfo.TotalSize - driveInfo.TotalFreeSpace;
                int driveUsage = (int)((100.0 / driveInfo.TotalSize) * usedSpace);

                SendMonitorValue(topic + "/TotalSize", GetSize(driveInfo.TotalSize).ToString(), configKey);
                SendMonitorValue(topic + "/AvailableFreeSpace", GetSize(driveInfo.AvailableFreeSpace).ToString(), configKey);
                SendMonitorValue(topic + "/TotalFreeSpace", GetSize(driveInfo.TotalFreeSpace).ToString(), configKey);
                SendMonitorValue(topic + "/UsedSpace", GetSize(usedSpace).ToString(), configKey);

                SendMonitorValue(topic + "/DriveFormat", driveInfo.DriveFormat, configKey);
                SendMonitorValue(topic + "/DriveUsage", driveUsage.ToString(), configKey);
                SendMonitorValue(topic + "/VolumeLabel", driveInfo.VolumeLabel, configKey);
            }
        }

        private void SendCurrentUserInfo()
        {
            const string configKey = "CurrentUser";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

            SendMonitorValue("Stats/CurrentUser", _currentUser, configKey);
        }

        private void RequestAgentIdleTime()
        {
            const string configKey = "IdleTime";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_IDLE_TIME;

            GetManager().SendAgentRequest(this, addonData, PlatformHelper.GetCurrentUsername());
        }

        private void RequestAgentDisplayInfo()
        {
            const string configKey = "Display-Info";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_INFORMATION;

            GetManager().SendAgentRequest(this, addonData);
        }

        private void RequestAgentDisplayScreenshot()
        {
            const string configKey = "Display-Screenshot";
            if (!CanRun(configKey))
                return;

            LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

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
            if (string.Compare(PlatformHelper.GetCurrentUsername(), username) != 0)
                return;

            const string configKey = "IdleTime";
            uint idleTime = (uint)data.requestData;

            SendMonitorValue("Stats/IdleTime", idleTime.ToString(), configKey);
        }

        private void ParseDisplayInfo(dynamic data, string username)
        {
            const string configKey = "Display-Info";
            List<DisplayInfo> displayInfos = data.requestData.ToObject<List<DisplayInfo>>();
            for (var i = 0; i < displayInfos.Count; i++)
            {
                DisplayInfo displayInfo = displayInfos[i];

                string topic = string.Format("Stats/Display/{0}", i);
                SendMonitorValue(topic + "/ScreenWidth", displayInfo.ScreenWidth.ToString(), configKey);
                SendMonitorValue(topic + "/ScreenHeight", displayInfo.ScreenHeight.ToString(), configKey);
            }
        }

        private void ParseDisplayScreenshot(dynamic data, string username)
        {
            int displayIndex = data.requestData.displayIndex;
            byte[] displayScreen = data.requestData.displayScreen;
            string topic = string.Format("Stats/Display/{0}/Screen", displayIndex);

            GetManager().PublishMessage(this, topic, displayScreen);
        }

        private double GetSize(long sizeInBytes)
        {
            switch (_config.SizeFormat)
            {
                default:
                case "MB":
                    return MathHelper.BytesToMegabytes(sizeInBytes);

                case "GB":
                    return MathHelper.BytesToGigabytes(sizeInBytes);

                case "TB":
                    return MathHelper.BytesToTerabytes(sizeInBytes);
            }
        }

        private bool CanRun(string configKey)
        {
            if (_config.Monitors == null || !_config.Monitors.ContainsKey(configKey) || !_config.Monitors[configKey].Enabled)
            {
                LoggerHelper.Verbose("{0} monitor disabled.", configKey);
                return false;
            }

            MonitorConfig monitor = _config.Monitors[configKey];
            if ((_monitorCounter % monitor.Interval) != 0)
                return false;

            return true;
        }

        private void SendMonitorValue(string topic, string value, string configKey = null)
        {
            if (configKey != null && _config.Monitors != null && _config.Monitors.ContainsKey(configKey) && _config.Monitors[configKey].Cacheable)
            {
                if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                    return;

                _cache[topic] = value;
            }

            GetManager().PublishMessage(this, topic, value);
        }
    }
}
