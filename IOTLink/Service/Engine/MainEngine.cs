using IOTLink.Service.Engine;
using IOTLink.Service.WSServer;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkAPI.Platform.Windows;
using IOTLinkService.Engine.MQTT;
using System;
using System.Collections.Generic;
using System.IO;
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
            _processMonitorTimer.Interval = 5 * 1000;
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
            AgentManager.GetInstance().StopAgents();
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
            {
                AgentManager agentManager = AgentManager.GetInstance();
                agentManager.StartAgent(sessionId, username);
            }

            AddonManager addonsManager = AddonManager.GetInstance();
            addonsManager.Raise_OnSessionChange(this, args);
        }

        private void StartAgents()
        {
            AgentManager agentManager = AgentManager.GetInstance();
            List<WindowsSessionInfo> winSessions = WindowsAPI.GetWindowsSessions().FindAll(s => s.IsActive);
            foreach (WindowsSessionInfo sessionInfo in winSessions)
            {
                agentManager.StartAgent(sessionInfo.SessionID, sessionInfo.Username);
            }
        }
    }
}
