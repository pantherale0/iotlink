using IOTLink.Service.WSServer;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkService.Engine.MQTT;
using System;
using System.Collections.Generic;
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

        private ManagementEventWatcher _processStartWatcher;
        private ManagementEventWatcher _processStopWatcher;

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

            // Process Start Watcher
            _processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            _processStartWatcher.EventArrived += OnProcessStarted;
            _processStartWatcher.Start();

            // Process Stop Watcher
            _processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            _processStopWatcher.EventArrived += OnProcessStopped;
            _processStopWatcher.Start();
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            string processName = (string)e.NewEvent.Properties["ProcessName"].Value;
            int sessionId = (int)e.NewEvent.Properties["SessionID"].Value;

            LoggerHelper.Debug("Process Started - SessionId: {0} ProcessName: {1}", processName);
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            string processName = (string)e.NewEvent.Properties["ProcessName"].Value;
            int sessionId = (int)e.NewEvent.Properties["SessionID"].Value;

            LoggerHelper.Debug("Process Stopped - SessionId: {0} ProcessName: {1}", processName);
        }

        public void StartApplication()
        {
            SetupMQTTHandlers();
            SetupWebSocket();
        }

        public void StopApplication()
        {
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

        public void OnSessionChange(string username, SessionChangeReason reason)
        {
            SessionChangeEventArgs args = new SessionChangeEventArgs
            {
                Username = username,
                Reason = reason
            };

            AddonManager addonsManager = AddonManager.GetInstance();
            addonsManager.Raise_OnSessionChange(this, args);
        }
    }
}
