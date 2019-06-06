using System;
using System.IO;
using System.ServiceProcess;
using IOTLink.Configs;
using IOTLink.Engine.MQTT;
using IOTLink.Engine.System;
using IOTLink.Helpers;

namespace IOTLink.Engine
{
    public class MainEngine
    {
        private static MainEngine _instance;
        private bool _addonsLoaded;

        private FileSystemWatcher _configWatcher;
        private DateTime _lastConfigChange;

        public static MainEngine GetInstance()
        {
            if (_instance == null)
                _instance = new MainEngine();

            return _instance;
        }

        private MainEngine()
        {
            _configWatcher = new FileSystemWatcher(PathHelper.DataPath(), "configuration.yaml");
            _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _configWatcher.Changed += OnConfigChanged;
            _configWatcher.Created += OnConfigChanged;
            _configWatcher.EnableRaisingEvents = true;
        }

        internal void StartApplication()
        {
            SetupMQTTHandlers();
        }

        internal void StopApplication()
        {
            LoggerHelper.GetInstance().Flush();
        }

        private void SetupMQTTHandlers()
        {
            ApplicationConfig config = ConfigHelper.GetApplicationConfig(true);
            if (config.MQTT == null)
            {
                LoggerHelper.Warn(typeof(MainEngine), "MQTT is disabled or not configured yet.");
                return;
            }

            MQTTClient client = MQTTClient.GetInstance();
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
                LoggerHelper.Info(typeof(MainEngine), "Changes to configuration.yaml detected. Reloading.");

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

        internal void OnSessionChange(string username, SessionChangeReason reason)
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
