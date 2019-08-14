using IOTLinkAddon.Common;
using IOTLinkAddon.Service.Monitors;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

namespace IOTLinkAddon.Service
{
    public class WindowsMonitorService : ServiceAddon
    {
        private static readonly int DEFAULT_INTERVAL = 60;

        private static readonly string DEFAULT_FORMAT_DISK_SIZE = "GB";
        private static readonly string DEFAULT_FORMAT_MEMORY_SIZE = "MB";

        private static readonly string DEFAULT_FORMAT_DATE = "yyyy-MM-dd";
        private static readonly string DEFAULT_FORMAT_TIME = "HH:mm:ss";
        private static readonly string DEFAULT_FORMAT_DATETIME = "yyyy-MM-dd HH:mm:ss";

        private Timer _monitorTimer;
        private uint _monitorCounter = 0;

        private string _configPath;
        private Configuration _config;

        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        private readonly List<IMonitor> monitors = new List<IMonitor>()
        {
            new CPUMonitor(),
            new MemoryMonitor(),
            new PowerMonitor(),
            new MediaMonitor(),
            new NetworkMonitor(),
            new StorageMonitor(),
            new SystemMonitor(),
            new UptimeMonitor(),
            new DisplayMonitor()
        };

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            var cfgManager = ConfigurationManager.GetInstance();
            _configPath = Path.Combine(this._currentPath, "config.yaml");
            _config = cfgManager.GetConfiguration(_configPath);
            cfgManager.SetReloadHandler(_configPath, OnConfigReload);

            OnSessionChangeHandler += OnSessionChange;
            OnConfigReloadHandler += OnConfigReload;
            OnAgentResponseHandler += OnAgentResponse;
            OnMQTTConnectedHandler += OnClearEvent;
            OnRefreshRequestedHandler += OnClearEvent;

            SetupTimers();
        }

        private void OnClearEvent(object sender, EventArgs e)
        {
            LoggerHelper.Verbose("Event {0} Received. Clearing cache and resending information.", e.GetType().ToString());

            _cache.Clear();
            SendAllInformation();
        }

        private void SetupTimers()
        {
            if (_config == null || !_config.GetValue("enabled", false))
            {
                LoggerHelper.Info("System monitor is disabled.");
                if (_monitorTimer != null)
                {
                    _monitorTimer.Stop();
                    _monitorTimer = null;
                }

                return;
            }

            if (_monitorTimer == null)
            {
                _monitorTimer = new Timer();
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

            LoggerHelper.Verbose("Reloading configuration");

            _config = ConfigurationManager.GetInstance().GetConfiguration(_configPath);
            SetupTimers();
        }

        private void OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            LoggerHelper.Verbose("OnSessionChange - {0}: {1}", e.Reason.ToString(), e.Username);

            GetManager().PublishMessage(this, e.Reason.ToString(), e.Username);
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Debug("OnMonitorTimerElapsed: Started");

            SendAllInformation();

            if (_monitorCounter++ == uint.MaxValue)
                _monitorCounter = 0;

            LoggerHelper.Debug("OnMonitorTimerElapsed: Completed");
        }

        private void SendAllInformation()
        {
            try
            {
                _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping

                // Execute Monitors
                foreach (IMonitor monitor in monitors)
                    ExecuteMonitor(monitor);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("SendAllInformation - Error: {0}", ex.ToString());
            }
            finally
            {
                _monitorTimer.Start(); // After everything, start the timer again.
            }
        }

        private void ExecuteMonitor(IMonitor monitor)
        {
            // Execute agent requests first
            Dictionary<string, AddonRequestType> agentRequests = monitor.GetAgentRequests();
            if (agentRequests != null)
            {
                foreach (KeyValuePair<string, AddonRequestType> entry in agentRequests)
                {
                    if (!CanRun(entry.Key))
                        continue;

                    LoggerHelper.Debug("{0} Monitor - Requesting Agent information", entry.Key);

                    dynamic addonData = new ExpandoObject();
                    addonData.requestType = entry.Value;

                    GetManager().SendAgentRequest(this, addonData);
                }
            }

            // Get monitor sensors
            string configKey = monitor.GetConfigKey();
            if (CanRun(configKey))
            {
                LoggerHelper.Debug("{0} Monitor - Sending information", configKey);

                List<MonitorItem> items = monitor.GetMonitorItems(_config, GetMonitorInterval(configKey));
                if (items == null)
                    return;

                foreach (MonitorItem item in items)
                    PublishItem(item);
            }
        }

        private void PublishItem(MonitorItem item)
        {
            if (item == null || item.Value == null || string.IsNullOrWhiteSpace(item.Topic))
                return;

            string value = string.Empty;
            switch (item.Type)
            {
                case MonitorItemType.TYPE_DISK_SIZE:
                    value = FormatSizeObject(item.ConfigKey, "diskFormat", DEFAULT_FORMAT_DISK_SIZE, item.Value);
                    break;

                case MonitorItemType.TYPE_MEMORY_SIZE:
                    value = FormatSizeObject(item.ConfigKey, "memoryFormat", DEFAULT_FORMAT_MEMORY_SIZE, item.Value);
                    break;

                case MonitorItemType.TYPE_NETWORK_SPEED:
                case MonitorItemType.TYPE_NETWORK_SIZE:
                    value = item.Value.ToString();
                    break;

                case MonitorItemType.TYPE_DATE:
                    value = FormatDateObject(item.Value, GetMonitorFormat(item.ConfigKey, "dateFormat", DEFAULT_FORMAT_DATE));
                    break;
                case MonitorItemType.TYPE_TIME:
                    value = FormatDateObject(item.Value, GetMonitorFormat(item.ConfigKey, "timeFormat", DEFAULT_FORMAT_TIME));
                    break;
                case MonitorItemType.TYPE_DATETIME:
                    value = FormatDateObject(item.Value, GetMonitorFormat(item.ConfigKey, "dateTimeFormat", DEFAULT_FORMAT_DATETIME));
                    break;

                case MonitorItemType.TYPE_UPTIME:
                    TimeSpan uptime = TimeSpan.FromSeconds(MathHelper.ToDouble(item.Value));
                    value = uptime.ToString(@"dd\:hh\:mm\:ss", CultureInfo.InvariantCulture);
                    break;

                case MonitorItemType.TYPE_RAW:
                default:
                    value = item.Value.ToString();
                    break;

                case MonitorItemType.TYPE_RAW_BYTES:
                    GetManager().PublishMessage(this, item.Topic, (byte[])item.Value);
                    return;
            }

            SendMonitorValue(item.Topic, value, item.ConfigKey);
        }

        private string FormatSizeObject(string configKey, string formatKey, string defaultFormat, object value)
        {
            long sizeInBytes = MathHelper.ToLong(value, 0L);
            string formatStr = GetMonitorFormat(configKey, formatKey, defaultFormat);

            string format = formatStr.Contains(":") ? formatStr.Split(':')[0] : formatStr;
            int roundDigits = formatStr.Contains(":") ? MathHelper.ToInteger(formatStr.Split(':')[1]) : 0;

            double size = UnitsHelper.ConvertSize(sizeInBytes, format);
            return Math.Round(size, roundDigits).ToString(CultureInfo.InvariantCulture);
        }

        private string FormatDateObject(object value, string format)
        {
            if (value is DateTime)
                return ((DateTime)value).ToString(format, CultureInfo.InvariantCulture);

            if (value is DateTimeOffset)
                return ((DateTimeOffset)value).ToString(format, CultureInfo.InvariantCulture);

            return value.ToString();
        }

        private void OnAgentResponse(object sender, AgentAddonResponseEventArgs e)
        {
            foreach (IMonitor monitor in monitors)
            {
                Dictionary<string, AddonRequestType> agentRequests = monitor.GetAgentRequests();
                if (agentRequests == null || agentRequests.Count == 0)
                    continue;

                AddonRequestType requestType = (AddonRequestType)e.Data.requestType;
                string configKey = agentRequests.FirstOrDefault(x => x.Value == requestType).Key;
                if (string.IsNullOrWhiteSpace(configKey) || !CanRun(configKey))
                    continue;

                List<MonitorItem> items = monitor.OnAgentResponse(requestType, e.Data, e.Username);
                if (items == null)
                    continue;

                foreach (MonitorItem item in items)
                    PublishItem(item);
            }
        }

        private bool CanRun(string configKey)
        {
            if (!GetMonitorEnabled(configKey))
            {
                LoggerHelper.Verbose("{0} monitor disabled.", configKey);
                return false;
            }

            if ((_monitorCounter % GetMonitorInterval(configKey)) != 0)
                return false;

            return true;
        }

        private string GetMonitorFormat(string configKey, string formatKey, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(configKey) || string.IsNullOrWhiteSpace(formatKey))
                return defaultValue;

            string value = _config.GetValue($"monitors:{configKey}:formats:{formatKey}", null);
            if (string.IsNullOrWhiteSpace(value))
                value = _config.GetValue($"formats:{formatKey}", defaultValue);

            return value;
        }

        private bool GetMonitorEnabled(string configKey)
        {
            if (string.IsNullOrWhiteSpace(configKey))
                return false;

            return _config.GetValue($"monitors:{configKey}:enabled", false);
        }

        private bool GetMonitorCacheable(string configKey)
        {
            if (string.IsNullOrWhiteSpace(configKey))
                return false;

            return _config.GetValue($"monitors:{configKey}:cacheable", false);
        }

        private int GetMonitorInterval(string configKey)
        {
            if (string.IsNullOrWhiteSpace(configKey))
                return DEFAULT_INTERVAL;

            return _config.GetValue($"monitors:{configKey}:interval", DEFAULT_INTERVAL);
        }

        private void SendMonitorValue(string topic, string value, string configKey = null)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return;

            if (GetMonitorCacheable(configKey))
            {
                if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                    return;

                _cache[topic] = value;
            }

            GetManager().PublishMessage(this, topic, value);
        }
    }
}