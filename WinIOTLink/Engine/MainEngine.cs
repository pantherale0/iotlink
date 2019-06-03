using System;
using System.ServiceProcess;
using WinIOTLink.Configs;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Helpers;
using static WinIOTLink.Helpers.LoggerHelper;

namespace WinIOTLink.Engine
{
    public class MainEngine
    {
        private static MainEngine _instance;
        private bool _addonsLoaded;

        public static MainEngine GetInstance()
        {
            if (_instance == null)
                _instance = new MainEngine();

            return _instance;
        }

        private MainEngine()
        {

        }

        public void StartApplication(ApplicationConfig applicationConfig)
        {
            if (applicationConfig.MQTT == null)
            {
                LoggerHelper.Error("MainEngine", "MQTT is disabled. Nothing to do.");
                return;
            }

            MQTTClient client = MQTTClient.GetInstance();
            client.Init(applicationConfig.MQTT);
            client.OnMQTTConnected += OnMQTTConnected;
            client.OnMQTTDisconnected += OnMQTTDisconnected;
            client.OnMQTTMessageReceived += OnMQTTMessageReceived;

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

        private void OnSessionChange(string username, SessionChangeReason reason)
        {
            LoggerHelper.WriteToFile("OnSessionChange", String.Format("{0}: {1}", username, reason.ToString()), LogLevel.INFO);

            MQTTClient.GetInstance().PublishMessage(String.Format("{0}/{1}", "SessionMonitor", reason.ToString()), username);
        }
    }
}
