using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkService.Service.Engine.MQTT;
using IOTLinkService.Service.WSServer;
using System;
using System.ServiceProcess;

namespace IOTLinkService.Service.Engine
{
    public class ServiceMain
    {
        private static ServiceMain _instance;
        private bool _addonsLoaded;

        private DateTime _lastConfigChange;

        private System.Timers.Timer _processMonitorTimer;

        public static ServiceMain GetInstance()
        {
            if (_instance == null)
                _instance = new ServiceMain();

            return _instance;
        }

        private ServiceMain()
        {
            LoggerHelper.Trace("ServiceMain instance created.");
            ConfigHelper.SetEngineConfigReloadHandler(OnConfigChanged);
        }

        private void OnProcessMonitorTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _processMonitorTimer.Stop();
            AgentManager.GetInstance().StartAgents();
            _processMonitorTimer.Start();
        }

        public void StartApplication()
        {
            if (_processMonitorTimer == null)
            {
                // Agent Monitor
                _processMonitorTimer = new System.Timers.Timer();
                _processMonitorTimer.Interval = 5 * 1000;
                _processMonitorTimer.Elapsed += OnProcessMonitorTimerElapsed;
                _processMonitorTimer.Start();
            }

            SetupMQTTHandlers();
            SetupWebSocket();
        }

        public void StopApplication()
        {
            AgentManager.GetInstance().StopAgents();
            LoggerHelper.GetInstance().Flush();
        }

        private void SetupWebSocket()
        {
            WebSocketServerManager webSocketManager = WebSocketServerManager.GetInstance();
            webSocketManager.Init();
        }

        private void SetupMQTTHandlers()
        {
            ApplicationConfig config = ConfigHelper.GetEngineConfig();
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
            {
                LoggerHelper.Verbose("Disconnecting from MQTT Broker");
                client.Disconnect();
            }

            client.Init(config.MQTT);
            client.OnMQTTConnected += OnMQTTConnected;
            client.OnMQTTDisconnected += OnMQTTDisconnected;
            client.OnMQTTMessageReceived += OnMQTTMessageReceived;
        }

        private void OnConfigChanged(object sender, ConfigReloadEventArgs e)
        {
            if (_lastConfigChange == null || _lastConfigChange.AddSeconds(1) <= DateTime.Now)
            {
                LoggerHelper.Info("Changes to configuration.yaml detected. Reloading.");
                ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();

                SetupMQTTHandlers();
                SetupWebSocket();
                addonsManager.Raise_OnConfigReloadHandler(this, e);

                _lastConfigChange = DateTime.Now;
            }
        }

        private void OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            LoggerHelper.Verbose("MQTT Connected");

            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            if (!_addonsLoaded)
            {
                addonsManager.LoadAddons();
                _addonsLoaded = true;
            }

            addonsManager.Raise_OnMQTTConnected(sender, e);
        }

        private void OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            LoggerHelper.Verbose("MQTT Disconnected");
            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnMQTTDisconnected(sender, e);
        }

        private void OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("MQTT Message Received");
            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnMQTTMessageReceived(sender, e);
        }

        public void OnSessionChange(string username, int sessionId, SessionChangeReason reason)
        {
            LoggerHelper.Verbose("Session Changed");

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

            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnSessionChange(this, args);
        }
    }
}
