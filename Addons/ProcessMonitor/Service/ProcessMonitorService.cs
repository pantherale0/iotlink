using IOTLinkAddon.Common;
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
using System.Dynamic;
using System.Globalization;
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

            LoggerHelper.Verbose("ProcessMonitorService::Init() - Started");

            var cfgManager = ConfigurationManager.GetInstance();
            _configPath = Path.Combine(_currentPath, "config.yaml");
            _config = cfgManager.GetConfiguration(_configPath);
            cfgManager.SetReloadHandler(_configPath, OnConfigReload);

            OnConfigReloadHandler += OnConfigReload;
            OnMQTTConnectedHandler += OnClearEvent;
            OnRefreshRequestedHandler += OnClearEvent;
            OnAgentResponseHandler += OnAgentResponse;

            SetupEventMonitor();
            Restart();

            LoggerHelper.Verbose("ProcessMonitorService::Init() - Completed");
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
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitors() - Monitoring list is empty.");
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
            if (!config.General.Enabled)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitor({0}) - Monitoring is disabled.", monitorConfiguration.Key);
                return;
            }

            ProcessMonitor monitor = new ProcessMonitor { Name = config.Key, Config = config };
            bool hasMonitor = MonitorHelper.GetProcessMonitorByName(_monitors, monitor.Name) != null;
            if (hasMonitor)
            {
                LoggerHelper.Warn("ProcessMonitorService::SetupMonitor({0}) - Duplicated Key. Ignoring.", monitorConfiguration.Key);
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
            LoggerHelper.Verbose("ProcessMonitorService::OnClearEvent() - Clearing cache and resending information.");

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
            LoggerHelper.Verbose("ProcessMonitorService::OnMonitorTimerElapsed() - Started");

            ExecuteMonitors();

            if (_monitorCounter++ == uint.MaxValue)
                _monitorCounter = 0;

            LoggerHelper.Verbose("ProcessMonitorService::OnMonitorTimerElapsed() - Completed");
        }

        private void ExecuteMonitors()
        {
            StopTimers();

            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitors() - Started");
            foreach (ProcessMonitor monitor in _monitors)
            {
                try
                {
                    var interval = monitor.Config.General.Interval;
                    if ((_monitorCounter % interval) != 0)
                        continue;

                    ExecuteMonitor(monitor);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("ProcessMonitorService::ExecuteMonitors({0}) - Exception: {1}", monitor.Name, ex);
                }
            }
            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitors() - Completed");

            RestartTimers();
        }

        private void ExecuteMonitor(ProcessMonitor monitor)
        {
            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitor({0}) - Started", monitor.Name);

            var config = monitor.Config;
            var processes = new List<ProcessInformation>();
            foreach (string processName in config.ProcessNames)
            {
                processes.AddRange(ProcessHelper.GetProcessesByName(processName));
            }

            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitor({0}) - {1} Processes Found", monitor.Name, processes.Count);
            if (processes.Count == 0)
            {
                LoggerHelper.Info("ProcessMonitorService::ExecuteMonitor({0}) - Not Running", monitor.Name);
                ProcessSingleProcess(monitor);
                return;
            }

            if (processes.Count > 0)
                ProcessGroup(monitor, processes);
            else
                ProcessSingleProcess(monitor, processes[0]);

            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitor({0}) - Completed", monitor.Name);
        }

        private void ProcessGroup(ProcessMonitor monitor, List<ProcessInformation> processes)
        {
            LoggerHelper.Verbose("ProcessMonitorService::ProcessGroup({0}) - Started", monitor.Name);

            List<ProcessInformation> mainProcesses = new List<ProcessInformation>();
            foreach (ProcessInformation item in processes)
            {
                ProcessInformation pi = item;
                while (pi.Parent != null && IsChildProcess(pi))
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

            LoggerHelper.Verbose("ProcessMonitorService::ProcessGroup({0}) - Completed", monitor.Name);
        }

        private void ProcessSingleProcess(ProcessMonitor monitor, ProcessInformation processInfo = null)
        {
            LoggerHelper.Verbose("ProcessMonitorService::ProcessSingleProcess({0}) - Started", monitor.Name);

            if (processInfo == null)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Setting empty process", monitor.Name);
                processInfo = new ProcessInformation { ProcessName = monitor.Name, Status = ProcessState.NotRunning };
            }

            var topic = $"Processes/{monitor.Name}";
            if (processInfo.Status == ProcessState.NotRunning)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Process not running.", monitor.Name);
                SendMonitorValue(monitor, $"{topic}/State", "OFF");
                SendMonitorValue(monitor, $"{topic}/Sensor", "{}");
                return;
            }

            if (processInfo.Status == ProcessState.Running && !MonitorHelper.HasConditions(monitor))
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Process Running - Monitor without rules.", monitor.Name);
                SendMonitorValue(monitor, $"{topic}/State", "ON");
            }

            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_PROCESS_INFORMATION;
            addonData.processId = processInfo.Id;

            GetManager().SendAgentRequest(this, addonData);

            LoggerHelper.Verbose("ProcessMonitorService::ProcessSingleProcess({0}) - Completed", monitor.Name);
        }

        private void OnAgentResponse(object sender, AgentAddonResponseEventArgs e)
        {
            AddonRequestType requestType = (AddonRequestType)e.Data.requestType;
            if (requestType != AddonRequestType.REQUEST_PROCESS_INFORMATION)
                return;

            ProcessInformation processInfo = e.Data.requestData.ToObject<ProcessInformation>();
            OnProcessInformationReceived(processInfo);
        }

        private void OnProcessInformationReceived(ProcessInformation processInfo)
        {
            if (processInfo == null)
                return;

            List<ProcessMonitor> monitors = MonitorHelper.GetProcessMonitorsByProcessInfo(_monitors, processInfo);

            LoggerHelper.Info("ProcessMonitorService::OnProcessInformationReceived({0}) - {1} Monitors found.", processInfo.Id, monitors.Count);
            if (monitors.Count == 0)
            {
                LoggerHelper.Info("ProcessMonitorService::OnProcessInformationReceived({0}) - Monitoring not found", processInfo.Id);
                return;
            }

            foreach (ProcessMonitor monitor in monitors)
            {
                LoggerHelper.Info("ProcessMonitorService::OnProcessInformationReceived({0}) - Monitor: {1}", processInfo.Id, monitor.Name);
                SendProcessInformation(monitor, processInfo);
            }
        }

        private void SendProcessInformation(ProcessMonitor monitor, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return;

            LoggerHelper.Info("ProcessMonitorService::SendProcessInformation({0}) - {1}", monitor.Name, JsonConvert.SerializeObject(processInfo));

            var topic = $"Processes/{monitor.Name}";
            var filter = MonitorHelper.CheckMonitorFilters(monitor, processInfo);
            var value = "{}";
            var state = "OFF";

            if (processInfo.Status != ProcessState.Running || !filter)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Setting empty process", monitor.Name);

                SendMonitorValue(monitor, $"{topic}/State", state);
                SendMonitorValue(monitor, $"{topic}/Sensor", value);
                return;
            }

            if (processInfo.Status == ProcessState.Running)
            {
                var mqttObject = CreateProcessInfoMQTT(monitor, processInfo);
                value = JsonConvert.SerializeObject(mqttObject);
                state = "ON";
            }

            SendMonitorValue(monitor, $"{topic}/State", state);
            SendMonitorValue(monitor, $"{topic}/Sensor", value);
        }

        private void OnProcessStarted(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessName);

            List<ProcessMonitor> monitors = MonitorHelper.GetProcessMonitorsByProcessName(_monitors, processName);

            LoggerHelper.Info("ProcessMonitorService::OnProcessStarted({0}) - {1} Monitors found.", processName, monitors.Count);
            if (monitors.Count == 0)
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

            if (pi.Parent != null && IsChildProcess(pi))
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Spawning child {1} from {0}", processName, pi.Id, pi.Parent.Id);
                return;
            }

            LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Process Detected (PID: {1} - Parent: {2})", processName, e.ProcessId, e.ParentProcessId);

            foreach (ProcessMonitor monitor in monitors)
                ProcessSingleProcess(monitor, pi);
        }

        private void OnProcessStopped(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessName);

            List<ProcessMonitor> monitors = MonitorHelper.GetProcessMonitorsByProcessName(_monitors, processName);

            LoggerHelper.Info("ProcessMonitorService::OnProcessStarted({0}) - {1} Monitors found.", processName, monitors.Count);
            foreach (ProcessMonitor monitor in monitors)
                ExecuteMonitor(monitor);
        }

        private bool IsChildProcess(ProcessInformation processInfo)
        {
            return (processInfo.Parent != null) && (string.Compare(processInfo.ProcessName, processInfo.Parent.ProcessName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private ProcessInfoMQTT CreateProcessInfoMQTT(ProcessMonitor monitor, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return null;

            var uptime = (int)(DateTime.Now - processInfo.StartDateTime).TotalSeconds;

            ProcessInfoMQTT pi = new ProcessInfoMQTT
            {
                Id = processInfo.Id.ToString(),
                SessionId = processInfo.SessionId.ToString(),
                ProcessName = processInfo.ProcessName,
                MainWindowTitle = processInfo.MainWindowTitle,
                FullScreen = processInfo.FullScreen.ToString(),
                Windows = processInfo.Windows,
                ClassNames = processInfo.ClassNames,
                MemoryUsed = FormatSizeObject(processInfo.MemoryUsed)
            };

            if (monitor.LastUpdated != null && monitor.LastProcessorUsageTime > 0d)
            {
                var timeInterval = (DateTime.Now - processInfo.StartDateTime).TotalMilliseconds;
                var cpuUsage = MathHelper.ToInteger(Math.Round((monitor.LastProcessorUsageTime - processInfo.ProcessorUsageTime) / timeInterval), 0);
                pi.ProcessorUsage = cpuUsage.ToString();
            }

            monitor.LastProcessorUsageTime = processInfo.ProcessorUsageTime;
            monitor.LastUpdated = DateTime.Now;

            pi.Status = processInfo.Status.ToString();
            pi.StartDateTime = FormatDateObject(processInfo.StartDateTime);
            pi.Uptime = FormatUptime(uptime);

            return pi;
        }

        private string FormatSizeObject(object value)
        {
            long sizeInBytes = MathHelper.ToLong(value, 0L);
            string formatStr = _config.GetValue("formats:memoryFormat", "MB");

            string format = formatStr.Contains(":") ? formatStr.Split(':')[0] : formatStr;
            int roundDigits = formatStr.Contains(":") ? MathHelper.ToInteger(formatStr.Split(':')[1]) : 0;

            double size = UnitsHelper.ConvertSize(sizeInBytes, format);
            return Math.Round(size, roundDigits).ToString(CultureInfo.InvariantCulture);
        }

        private string FormatDateObject(object value)
        {
            var format = _config.GetValue("formats:dateTimeFormat", "yyyy-MM-dd HH:mm:ss");
            if (value is DateTime)
                return ((DateTime)value).ToString(format, CultureInfo.InvariantCulture);

            return value.ToString();
        }

        private string FormatUptime(int value)
        {
            var inSeconds = _config.GetValue("formats:uptimeInSeconds", false);
            if (inSeconds)
                return value.ToString();

            TimeSpan uptime = TimeSpan.FromSeconds(MathHelper.ToDouble(value));
            return uptime.ToString(@"dd\:hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        private void SendMonitorValue(ProcessMonitor monitor, string topic, string value)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return;

            if (monitor.Config.General.Cacheable)
            {
                if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                    return;

                _cache[topic] = value;
            }

            GetManager().PublishMessage(this, topic, value);
        }
    }
}