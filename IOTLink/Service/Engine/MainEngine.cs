using IOTLink.Service.WSServer;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkAPI.Platform.Windows;
using IOTLinkService.Engine.MQTT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;
using System.Threading;

namespace IOTLinkService.Engine
{
    public class MainEngine
    {
        private static MainEngine _instance;
        private bool _addonsLoaded;

        private FileSystemWatcher _configWatcher;
        private DateTime _lastConfigChange;

        private System.Timers.Timer _processMonitorTimer;

        private Dictionary<string, object> _globals = new Dictionary<string, object>();

        public static MainEngine GetInstance()
        {
            if (_instance == null)
                _instance = new MainEngine();

            return _instance;
        }

        private MainEngine()
        {
            // Configuration Watcher
            _configWatcher = new FileSystemWatcher(PathHelper.ConfigPath(), "configuration.yaml");
            _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _configWatcher.Changed += OnConfigChanged;
            _configWatcher.Created += OnConfigChanged;
            _configWatcher.EnableRaisingEvents = true;

            // Agent Monitor
            _processMonitorTimer = new System.Timers.Timer();
            _processMonitorTimer.Interval = 10 * 1000;
            _processMonitorTimer.Elapsed += OnProcessMonitorTimerElapsed;
            _processMonitorTimer.Start();
        }

        private void OnProcessMonitorTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StartAgents();
        }

        public void StartApplication()
        {
            SetupMQTTHandlers();
            SetupWebSocket();
            StartAgents();
        }

        public void StopApplication()
        {
            StopAgents();
            LoggerHelper.GetInstance().Flush();
        }

        public object GetGlobal(string key, object def = null)
        {
            if (!_globals.ContainsKey(key))
                return def;

            return _globals[key];
        }

        public void SetGlobal(string key, object value)
        {
            if (_globals.ContainsKey(key))
                _globals.Remove(key);

            _globals.Add(key, value);
        }

        private void SetupWebSocket()
        {
            WebSocketServerManager webSocketManager = WebSocketServerManager.GetInstance();
            webSocketManager.Init();
        }

        private void SetupMQTTHandlers()
        {
            ApplicationConfig config = ConfigHelper.GetEngineConfig(true);
            if (config == null)
            {
                LoggerHelper.Error("Configuration not loaded. Check your configuration file for mistakes and re-save it.");
                return;
            }

            if (config.MQTT == null)
            {
                LoggerHelper.Warn("MQTT is disabled or not configured yet.");
                return;
            }

            MQTTClient client = MQTTClient.GetInstance();
            if (client == null)
            {
                LoggerHelper.Error("Failed to obtain MQTTClient instance. Restart the service.");
                return;
            }

            if (client.IsConnected())
                client.Disconnect();

            client.Init(config.MQTT);
            client.OnMQTTConnected += OnMQTTConnected;
            client.OnMQTTDisconnected += OnMQTTDisconnected;
            client.OnMQTTMessageReceived += OnMQTTMessageReceived;
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            if (_lastConfigChange == null || _lastConfigChange.AddSeconds(1) <= DateTime.Now)
            {
                LoggerHelper.Info("Changes to configuration.yaml detected. Reloading.");
                Thread.Sleep(2500);

                SetupMQTTHandlers();
                AddonManager.GetInstance().Raise_OnConfigReloadHandler(this, EventArgs.Empty);

                _lastConfigChange = DateTime.Now;
            }
        }

        private void OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            AddonManager addonsManager = AddonManager.GetInstance();
            if (!_addonsLoaded)
            {
                addonsManager.LoadAddons();
                _addonsLoaded = true;
            }

            addonsManager.Raise_OnMQTTConnected(sender, e);
        }

        private void OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            AddonManager addonsManager = AddonManager.GetInstance();
            addonsManager.Raise_OnMQTTDisconnected(sender, e);
        }

        private void OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            AddonManager addonsManager = AddonManager.GetInstance();
            addonsManager.Raise_OnMQTTMessageReceived(sender, e);
        }

        public void OnSessionChange(string username, int sessionId, SessionChangeReason reason)
        {
            SessionChangeEventArgs args = new SessionChangeEventArgs
            {
                Username = username,
                SessionId = sessionId,
                Reason = reason
            };

            if (reason == SessionChangeReason.SessionLogon || reason == SessionChangeReason.SessionUnlock || reason == SessionChangeReason.RemoteConnect)
                StartAgent(username, sessionId);

            AddonManager addonsManager = AddonManager.GetInstance();
            addonsManager.Raise_OnSessionChange(this, args);
        }

        private void StartAgent(string username, int sessionId)
        {
            string wmiQuery = string.Format("SELECT SessionId, ProcessID, CommandLine FROM Win32_Process WHERE Name='{0}.exe'", PathHelper.APP_AGENT_NAME);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();

            LoggerHelper.Trace("StartAgent - Found {0} agent instances...", objCollection.Count);
            foreach (ManagementObject processInfo in objCollection)
            {
                int agentSessionId = Convert.ToInt32(processInfo.Properties["SessionId"].Value);
                string agentCommandLine = (string)processInfo.Properties["CommandLine"].Value;

                if (agentSessionId == sessionId && agentCommandLine.Contains("--agent"))
                {
                    LoggerHelper.Trace("StartAgent - Agent instance is already running for this user. Skipping.");
                    return;
                }
            }

            RunInfo runInfo = new RunInfo
            {
                Application = Path.Combine(PathHelper.BaseAppPath(), PathHelper.APP_AGENT_NAME + ".exe"),
                CommandLine = "--agent " + WebSocketServerManager.WEBSOCKET_URI,
                WorkingDir = PathHelper.BaseAppPath(),
                UserName = username,
                Visible = false,
                FallbackToFirstActiveUser = false
            };

            PlatformHelper.Run(runInfo);
        }

        private void StartAgents()
        {
            List<WindowsSessionInfo> winSessions = WindowsAPI.GetWindowsSessions().FindAll(s => s.IsActive);
            foreach (WindowsSessionInfo sessionInfo in winSessions)
            {
                StartAgent(sessionInfo.UserName, sessionInfo.SessionID);
            }
        }

        private void StopAgents()
        {
            string wmiQuery = string.Format("SELECT SessionId, ProcessID, CommandLine FROM Win32_Process WHERE Name='{0}.exe'", PathHelper.APP_AGENT_NAME);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();

            LoggerHelper.Trace("StopAgents - Found {0} agent instances...", objCollection.Count);
            foreach (ManagementObject processInfo in objCollection)
            {
                int agentSessionId = Convert.ToInt32(processInfo.Properties["SessionId"].Value);
                int agentProcessId = Convert.ToInt32(processInfo.Properties["ProcessID"].Value);
                string agentCommandLine = (string)processInfo.Properties["CommandLine"].Value;

                if (agentCommandLine.Contains("--agent"))
                {
                    LoggerHelper.Trace("StopAgents - Killing Session {0} Agent (PID: {1})", agentSessionId, agentProcessId);
                    try
                    {
                        Process process = Process.GetProcessById(agentProcessId);
                        if (process != null)
                            process.Kill();
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error("StopAgents - Failed to Kill PID {0}: {1}", agentProcessId, ex.ToString());
                    }
                }
            }
        }
    }
}
