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

            MQTTClient.GetInstance().Init(applicationConfig.MQTT);
        }

        internal void OnSessionChange(string username, SessionChangeReason reason)
        {
            LoggerHelper.WriteToFile("OnSessionChange", String.Format("{0}: {1}", username, reason.ToString()), LogLevel.INFO);

            MQTTClient.GetInstance().PublishMessage(String.Format("{0}/{1}", "SessionMonitor", reason.ToString()), username);
        }
    }
}
