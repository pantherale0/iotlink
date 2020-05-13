﻿using IOTLinkAddon.Common;
using IOTLinkAddon.Service.Monitors;
using IOTLinkAddon.Service.Platform;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private ProcessEventMonitor eventMonitor = new ProcessEventMonitor();

        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        private List<int> _monitored = new List<int>();

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

            SetupEventMonitor();
            SetupTimers();
        }

        private void SetupEventMonitor()
        {
            eventMonitor.Init();
            eventMonitor.OnProcessStarted += OnProcessStarted;
            eventMonitor.OnProcessStopped += OnProcessStopped;
        }

        private void SetupTimers()
        {
            if (_config == null || !_config.GetValue("enabled", false))
            {
                LoggerHelper.Info("ProcessMonitorService::SetupTimers() - Process monitor is disabled.");
                CleanTimers();
                return;
            }

            StartTimers();
        }

        private void CleanTimers()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer = null;
            }
        }

        private void StartTimers()
        {
            CleanTimers();

            if (_monitorTimer == null)
            {
                _monitorTimer = new Timer();
                _monitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimerElapsed);
            }

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
                string processName = GetMonitorName(monitor);
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

        private void OnProcessStarted(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessName);

            Configuration monitor = _config.GetConfigurationList("monitors").First(x => string.Compare(processName, GetMonitorName(x)) == 0);
            if (monitor == null)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Monitoring NOT FOUND (PID: {1} - Parent: {2})", processName, e.ProcessId, e.ParentProcessId);
                return;
            }

            ProcessInformation pi = ProcessHelper.GetProcessInformation(e.ProcessId);
            if (pi == null)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Unable to get information from PID {1}", processName, e.ProcessId);
                return;
            }

            if (pi.Parent != null && string.Compare(pi.ProcessName, pi.Parent.ProcessName) == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Spawning child {1} from {0}", processName, pi.Id, pi.Parent.Id);
                return;
            }

            LoggerHelper.Info("ProcessMonitorService::OnProcessStarted({0}) - FOUND Main Process (PID: {1} - Parent: {2})", processName, e.ProcessId, e.ParentProcessId);
            ProcessSingleProcess(monitor, pi);
        }

        private void OnProcessStopped(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessName);

            Configuration monitor = _config.GetConfigurationList("monitors").First(x => string.Compare(processName, GetMonitorName(x)) == 0);
            if (monitor == null)
            {
                LoggerHelper.Info("ProcessMonitorService::OnProcessStopped({0}) - Monitoring NOT FOUND (PID: {1} - Parent: {2})", processName, e.ProcessId, e.ParentProcessId);
                return;
            }

            if (!_monitored.Contains(e.ProcessId))
                return;

            ProcessSingleProcess(monitor);
        }

        private void ProcessMonitor(Configuration monitor)
        {
            int interval = monitor.GetValue("interval", DEFAULT_INTERVAL);
            if ((_monitorCounter % interval) != 0)
                return;

            string processName = GetMonitorName(monitor);
            bool enabled = monitor.GetValue("enabled", false);
            bool grouped = monitor.GetValue("grouped", false);

            LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Enabled: {1} - Grouped: {2}", processName, enabled, grouped);
            if (!enabled)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Monitoring is disabled. Skipping.", processName);
                return;
            }

            List<ProcessInformation> processes = ProcessHelper.GetProcessesByName(processName);
            if (processes.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Not Running", processName);
                ProcessSingleProcess(monitor);
                return;
            }

            LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - {1} Processes Found", processName, processes.Count);

            if (grouped)
                ProcessGroup(monitor, processes);
            else
                ProcessSingleProcess(monitor, processes[0]);
        }

        private void ProcessGroup(Configuration monitor, List<ProcessInformation> processes)
        {
            string processName = GetMonitorName(monitor);

            List<ProcessInformation> mainProcesses = new List<ProcessInformation>();
            foreach (ProcessInformation item in processes)
            {
                ProcessInformation pi = item;
                while (pi.Parent != null && string.Compare(pi.ProcessName, pi.Parent.ProcessName) == 0)
                {
                    LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Iterating over child process to find parent: {1}", processName, JsonConvert.SerializeObject(pi));
                    pi = pi.Parent;
                }

                if (!mainProcesses.Any(x => x.Id == pi.Id))
                    mainProcesses.Add(pi);
            }

            if (mainProcesses.Count == 0)
                return;

            ProcessSingleProcess(monitor, mainProcesses[0]);
        }

        private void ProcessSingleProcess(Configuration monitor, ProcessInformation process = null)
        {
            string processName = GetMonitorName(monitor);
            LoggerHelper.Info("ProcessMonitorService::ProcessSingleProcess({0})", processName);

            if (process == null)
                process = new ProcessInformation { ProcessName = processName, Status = ProcessState.NotRunning };

            string topic = $"Processes/{processName}";
            string value = process.Status == ProcessState.Running ? JsonConvert.SerializeObject(process) : "{}";
            string state = process.Status == ProcessState.Running ? "ON" : "OFF";

            if (process.Status == ProcessState.NotRunning)
                RemoveMonitoredProcess(process.Id);
            else
                AddMonitoredProcess(process.Id);

            SendMonitorValue(monitor, $"{topic}/State", state);
            SendMonitorValue(monitor, $"{topic}/Sensor", value);
        }

        private string GetMonitorName(Configuration monitor)
        {
            if (monitor == null)
                return null;

            return monitor.GetValue("name", string.Empty);
        }

        private void AddMonitoredProcess(int processId)
        {
            if (_monitored.Contains(processId))
                return;

            _monitored.Add(processId);
        }

        private void RemoveMonitoredProcess(int processId)
        {
            if (!_monitored.Contains(processId))
                return;

            _monitored.Remove(processId);
        }

        private void SendMonitorValue(Configuration monitor, string topic, string value)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return;

            bool cacheable = monitor.GetValue("cacheable", false);
            if (cacheable)
            {
                if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                    return;

                _cache[topic] = value;
            }

            GetManager().PublishMessage(this, topic, value);
        }
    }
}