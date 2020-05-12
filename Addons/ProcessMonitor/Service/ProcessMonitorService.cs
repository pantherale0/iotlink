using IOTLinkAddon.Service.Platform;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Timers;

namespace IOTLinkAddon.Service
{
    public class ProcessMonitorService : ServiceAddon
    {
        private static readonly int DEFAULT_INTERVAL = 60;

        private Timer _monitorTimer;
        private uint _monitorCounter = 0;

        private string _configPath;
        private Configuration _config;

        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            var cfgManager = ConfigurationManager.GetInstance();
            _configPath = Path.Combine(_currentPath, "config.yaml");
            _config = cfgManager.GetConfiguration(_configPath);
            cfgManager.SetReloadHandler(_configPath, OnConfigReload);

            OnConfigReloadHandler += OnConfigReload;
            OnMQTTConnectedHandler += OnClearEvent;
            OnRefreshRequestedHandler += OnClearEvent;

            SetupTimers();
        }

        private void SetupTimers()
        {
            if (_config == null || !_config.GetValue("enabled", false))
            {
                LoggerHelper.Info("ProcessMonitorService::SetupTimers() - Process monitor is disabled.");
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

            LoggerHelper.Info("ProcessMonitorService::SetupTimers() - System monitor is activated.");
        }

        private void OnClearEvent(object sender, EventArgs e)
        {
            LoggerHelper.Verbose("ProcessMonitorService::OnClearEvent() - Event {0} Received. Clearing cache and resending information.", e.GetType().ToString());

            _cache.Clear();
            SendAllInformation();
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            if (e.ConfigType != ConfigType.CONFIGURATION_ADDON)
                return;

            LoggerHelper.Verbose("ProcessMonitorService::OnConfigReload() - Reloading configuration");

            _config = ConfigurationManager.GetInstance().GetConfiguration(_configPath);
            SetupTimers();
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Debug("ProcessMonitorService::OnMonitorTimerElapsed() - Started");

            SendAllInformation();

            if (_monitorCounter++ == uint.MaxValue)
                _monitorCounter = 0;

            LoggerHelper.Debug("ProcessMonitorService::OnMonitorTimerElapsed() - Completed");
        }

        private void SendAllInformation()
        {
            _monitorTimer.Stop(); // Stop the timer in order to prevent overlapping

            List<Configuration> monitors = _config.GetConfigurationList("monitors");
            foreach (var monitor in monitors)
            {
                string processName = monitor.GetValue("name", string.Empty);
                try
                {
                    ProcessMonitor(monitor);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("ProcessMonitorService::SendAllInformation() - {0} Monitor - Error: {1}", processName, ex);
                }
            }

            _monitorTimer.Start(); // After everything, start the timer again.
        }

        private void ProcessMonitor(Configuration monitor)
        {
            string processName = monitor.GetValue("name", string.Empty);
            bool enabled = monitor.GetValue("enabled", false);
            bool cacheable = monitor.GetValue("cacheable", false);
            bool grouped = monitor.GetValue("grouped", false);

            LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Enabled: {1} - Cacheable: {2} - Grouped: {3}", processName, enabled, cacheable, grouped);
            if (!enabled)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Monitoring is disabled. Skipping.", processName);
                return;
            }

            List<ProcessInformation> processes = ProcessHelper.GetProcessesByName(processName);
            if (processes.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Not Running", processName);
                return;
            }

            LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - {1} Processes Found", processName, processes.Count);
            foreach (ProcessInformation pi in processes)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - {1}", processName, JsonConvert.SerializeObject(pi));
                if (grouped)
                {
                    LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Monitoring is grouped. Skipping other processes with same name.", processName);
                    break;
                }
            }
        }
    }
}