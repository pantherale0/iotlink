using System;
using System.IO;
using System.ServiceProcess;
using WinIOTLink.Configs;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Engine.System;
using WinIOTLink.Helpers;

namespace WinIOTLink.Engine
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
            _configWatcher.EnableRaisingEvents = true;
        }

        public void StartApplication()
        {
            ApplicationConfig config = ConfigHelper.GetApplicationConfig(true);
            if (config.MQTT == null)
            {
                LoggerHelper.Error(typeof(MainEngine), "MQTT is disabled. Nothing to do.");
                return;
            }

            MQTTClient client = MQTTClient.GetInstance();
            client.Init(config.MQTT);
            client.OnMQTTConnected += OnMQTTConnected;
            client.OnMQTTDisconnected += OnMQTTDisconnected;
            client.OnMQTTMessageReceived += OnMQTTMessageReceived;
        }

        internal void StopApplication()
        {
            LoggerHelper.GetInstance().Flush();
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            if (_lastConfigChange == null || _lastConfigChange.AddSeconds(1) <= DateTime.Now)
            {
                LoggerHelper.Info(typeof(MainEngine), "Changes to configuration.yaml detected. Reloading.");
                _lastConfigChange = DateTime.Now;

                MQTTClient client = MQTTClient.GetInstance();
                client.Disconnect();
                StartApplication();
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
