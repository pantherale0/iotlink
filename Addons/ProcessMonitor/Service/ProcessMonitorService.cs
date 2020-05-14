using IOTLinkAddon.Common.Configs;
using IOTLinkAddon.Common.Helpers;
using IOTLinkAddon.Common.Processes;
using IOTLinkAddon.Service.Monitors;
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
        private string _configPath;
        private Configuration _config;

        private List<ProcessMonitor> _monitors = new List<ProcessMonitor>();
        private ProcessEventMonitor _eventMonitor;

        private Timer _monitorTimer;
        private uint _monitorCounter = 0;

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

            SetupEventMonitor();
            Restart();
        }

        private bool IsAddonEnabled()
        {
            if (_config == null || !_config.GetValue("enabled", false))
            {
                CleanTimers();
                return false;
            }

            return true;
        }

        private void Restart()
        {
            if (!IsAddonEnabled())
                return;

            SetupMonitors();
            StartTimers();
        }

        private void ClearMonitors()
        {
            _monitors.Clear();
        }

        private void SetupEventMonitor()
        {
            if (_eventMonitor != null)
                _eventMonitor = null;

            _eventMonitor = new ProcessEventMonitor();
            _eventMonitor.Init();
            _eventMonitor.OnProcessStarted += OnProcessStarted;
            _eventMonitor.OnProcessStopped += OnProcessStopped;
        }

        private void SetupMonitors()
        {
            ClearMonitors();

            List<Configuration> monitorConfigurations = _config.GetConfigurationList("monitors");
            if (monitorConfigurations == null || monitorConfigurations.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitors() - The monitoring list is empty.");
                return;
            }

            foreach (Configuration monitorConfiguration in monitorConfigurations)
            {
                try
                {
                    SetupMonitor(monitorConfiguration);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Debug("ProcessMonitorService::SetupMonitors({0}): Error - {1}", monitorConfiguration.Key, ex);
                }
            }
        }

        private void SetupMonitor(Configuration monitorConfiguration)
        {
            MonitorConfig config = MonitorConfig.FromConfiguration(monitorConfiguration);
            if (!config.Monitoring.Enabled)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitor({0}) - Monitoring is Disabled.", monitorConfiguration.Key);
                return;
            }

            ProcessMonitor monitor = new ProcessMonitor { Name = config.Key, Config = config };
            bool hasMonitor = _monitors.Any(x => string.Compare(x.Name, monitor.Name, StringComparison.OrdinalIgnoreCase) == 0);
            if (hasMonitor)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitor({0}) - Duplicated Key.", monitorConfiguration.Key);
                return;
            }

            _monitors.Add(monitor);
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
            if (!IsAddonEnabled())
                return;

            CleanTimers();

            _monitorTimer = new Timer();
            _monitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimerElapsed);
            _monitorTimer.Interval = 1000;
            _monitorTimer.Start();
        }

        private void RestartTimers()
        {
            if (_monitorTimer == null)
                return;

            _monitorTimer.Stop();
            _monitorTimer.Start();
        }

        private void StopTimers()
        {
            if (_monitorTimer == null)
                return;

            _monitorTimer.Stop();
        }

        private void OnClearEvent(object sender, EventArgs e)
        {
            LoggerHelper.Verbose("ProcessMonitorService::OnClearEvent() - Event {0} Received. Clearing cache and resending information.", e.GetType().ToString());

            _cache.Clear();
            ExecuteMonitors();
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            if (e.ConfigType != ConfigType.CONFIGURATION_ADDON)
                return;

            LoggerHelper.Verbose("ProcessMonitorService::OnConfigReload() - Reloading configuration");

            _config = ConfigurationManager.GetInstance().GetConfiguration(_configPath);
            Restart();
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Debug("ProcessMonitorService::OnMonitorTimerElapsed() - Started");

            ExecuteMonitors();

            if (_monitorCounter++ == uint.MaxValue)
                _monitorCounter = 0;

            LoggerHelper.Debug("ProcessMonitorService::OnMonitorTimerElapsed() - Completed");
        }

        private void ExecuteMonitors()
        {
            StopTimers();

            foreach (ProcessMonitor monitor in _monitors)
            {
                try
                {
                    var interval = monitor.Config.Monitoring.Interval;
                    if ((_monitorCounter % interval) != 0)
                        continue;

                    ExecuteMonitor(monitor);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("ProcessMonitorService::ExecuteMonitors({0}) - Exception: {1}", monitor.Name, ex);
                }
            }

            RestartTimers();
        }

        private void ExecuteMonitor(ProcessMonitor monitor)
        {
            var config = monitor.Config;
            bool grouped = config.Monitoring.Grouped;

            List<ProcessInformation> processes = new List<ProcessInformation>();
            foreach (string processName in config.ProcessNames)
            {
                processes.AddRange(ProcessHelper.GetProcessesByName(processName));
            }

            if (processes.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::ExecuteMonitor({0}) - Not Running", monitor.Name);
                ProcessSingleProcess(monitor);
                return;
            }

            LoggerHelper.Debug("ProcessMonitorService::ExecuteMonitor({0}) - {1} Processes Found", monitor.Name, processes.Count);

            if (grouped)
                ProcessGroup(monitor, processes);
            else
                ProcessSingleProcess(monitor, processes[0]);
        }

        private void ProcessGroup(ProcessMonitor monitor, List<ProcessInformation> processes)
        {
            List<ProcessInformation> mainProcesses = new List<ProcessInformation>();
            foreach (ProcessInformation item in processes)
            {
                ProcessInformation pi = item;
                while (pi.Parent != null && string.Compare(pi.ProcessName, pi.Parent.ProcessName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Iterating over child process to find parent: {1}", monitor.Name, JsonConvert.SerializeObject(pi));
                    pi = pi.Parent;
                }

                if (!mainProcesses.Any(x => x.Id == pi.Id))
                    mainProcesses.Add(pi);
            }

            if (mainProcesses.Count == 0)
                return;

            ProcessSingleProcess(monitor, mainProcesses[0]);
        }

        private void ProcessSingleProcess(ProcessMonitor monitor, ProcessInformation processInfo = null)
        {
            LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0})", monitor.Name);

            if (processInfo == null)
                processInfo = new ProcessInformation { ProcessName = monitor.Name, Status = ProcessState.NotRunning };

            string topic = $"Processes/{monitor.Name}";
            string value = processInfo.Status == ProcessState.Running ? JsonConvert.SerializeObject(processInfo) : "{}";
            string state = processInfo.Status == ProcessState.Running ? "ON" : "OFF";

            if (processInfo.Status == ProcessState.NotRunning)
                monitor.Process = null;
            else
                monitor.Process = processInfo;

            SendMonitorValue(monitor, $"{topic}/State", state);
            SendMonitorValue(monitor, $"{topic}/Sensor", value);
        }

        private void OnProcessStarted(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessName);

            ProcessMonitor monitor = _monitors.FirstOrDefault(x => x.Config.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase));
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

            LoggerHelper.Info("ProcessMonitorService::OnProcessStarted({0}) - Process Detected (PID: {1} - Parent: {2})", processName, e.ProcessId, e.ParentProcessId);
            ProcessSingleProcess(monitor, pi);
        }

        private void OnProcessStopped(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessName);

            ProcessMonitor monitor = _monitors.FirstOrDefault(x => x.Config.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase));
            if (monitor == null)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStopped({0}) - Monitoring NOT FOUND (PID: {1} - Parent: {2})", processName, e.ProcessId, e.ParentProcessId);
                return;
            }

            if (monitor.Process == null || monitor.Process.Id != e.ProcessId)
                return;

            ProcessSingleProcess(monitor);
        }

        private void SendMonitorValue(ProcessMonitor monitor, string topic, string value)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return;

            if (monitor.Config.Monitoring.Cacheable)
            {
                if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                    return;

                _cache[topic] = value;
            }

            GetManager().PublishMessage(this, topic, value);
        }
    }
}