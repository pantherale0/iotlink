﻿using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Threading.Tasks;
using System.Timers;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkService.Service.MQTT
{
    internal class MQTTClientManager : IDisposable
    {
        private static readonly int MINIMUM_DELAY_VERIFY_CONNECTION = 5;

        private static readonly int MONITOR_TIMER_INITAL_INTERVAL = 60 * 1000;
        private static readonly int MONITOR_TIMER_NORMAL_INTERVAL = 10 * 1000;

        private static MQTTClientManager _instance;

        private MQTTClient _mqttClient;

        private Timer _monitorTimer;

        private DateTime lastVerifyConnection = new DateTime(0L);

        private readonly object connectionLock = new object();
        private readonly object verifyLock = new object();

        public event MQTTEventHandler OnMQTTConnected;
        public event MQTTEventHandler OnMQTTDisconnected;
        public event MQTTMessageEventHandler OnMQTTMessageReceived;
        public event MQTTRefreshMessageEventHandler OnMQTTRefreshMessageReceived;

        public static MQTTClientManager GetInstance()
        {
            if (_instance == null)
                _instance = new MQTTClientManager();

            return _instance;
        }

        private MQTTClientManager()
        {
            if (_monitorTimer == null)
                _monitorTimer = new Timer();

            _monitorTimer.Interval = MONITOR_TIMER_INITAL_INTERVAL;
            _monitorTimer.Elapsed += OnMonitorTimeElapsed;
            _monitorTimer.AutoReset = true;
            _monitorTimer.Enabled = true;
        }

        public async void Start()
        {
            LoggerHelper.Info("MQTTClientManager::Start() - Initilizing MQTT");
            await Task.Run(() => Connect());
        }

        public async void Stop()
        {
            LoggerHelper.Info("MQTTClientManager::Stop() - Finishing MQTT");
            await Task.Run(() => Disconnect());
        }

        public void Dispose()
        {
            LoggerHelper.Verbose("MQTTClientManager::Dispose()");
            if (_mqttClient != null)
                _mqttClient.Disconnect();

            _mqttClient = null;
        }

        internal void CleanEvents()
        {
            OnMQTTConnected = null;
            OnMQTTDisconnected = null;
            OnMQTTMessageReceived = null;
            OnMQTTRefreshMessageReceived = null;
        }

        internal void BindEvents()
        {
            if (_mqttClient == null)
                return;

            _mqttClient.OnMQTTConnected += OnMQTTConnectedHandler;
            _mqttClient.OnMQTTDisconnected += OnMQTTDisconnectedHandler;
            _mqttClient.OnMQTTMessageReceived += OnMQTTMessageReceivedHandler;
            _mqttClient.OnMQTTRefreshMessageReceived += OnMQTTRefreshMessageReceivedHandler;
        }

        internal void PublishMessage(string addonTopic, string message)
        {
            LoggerHelper.Verbose("MQTTClientManager::PublishMessage('{0}', '{1}')", addonTopic, message);

            try
            {
                VerifyConnection();

                _mqttClient.PublishMessage(addonTopic, message);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::PublishMessage('{0}', '{1}') -> {2}", addonTopic, message, ex.Message);
                Disconnect(true);
            }
        }

        internal void PublishMessage(string topic, byte[] message)
        {
            LoggerHelper.Verbose("MQTTClientManager::PublishMessage('{0}', '{1}')", topic, message);

            try
            {
                VerifyConnection();

                _mqttClient.PublishMessage(topic, message);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::PublishMessage('{0}', '{1}') -> {2}", topic, message, ex.Message);
                Disconnect(true);
            }
        }

        internal void PublishDiscoveryMessage(string addonTopic, string preffixName, HassDiscoveryOptions discoveryOptions)
        {
            LoggerHelper.Verbose("MQTTClientManager::PublishDiscoveryMessage('{0}', '{1}', '{2}')", addonTopic, preffixName, discoveryOptions);

            try
            {
                VerifyConnection();

                _mqttClient.PublishDiscoveryMessage(addonTopic, preffixName, discoveryOptions);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::PublishDiscoveryMessage('{0}', '{1}', '{2}') -> {3}", addonTopic, preffixName, discoveryOptions, ex.Message);
                Disconnect(true);
            }
        }

        private void Connect()
        {
            lock (connectionLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::Connect()");
                if (_mqttClient == null)
                {
                    _mqttClient = MQTTClient.GetInstance();
                    _mqttClient.Init();
                }

                BindEvents();
                _mqttClient.Connect();
            }
        }

        private void Disconnect(bool skipLastWill = false)
        {
            lock (connectionLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::Disconnect({0})", skipLastWill);
                if (_mqttClient == null)
                    return;

                _mqttClient.CleanEvents();
                _mqttClient.Disconnect(skipLastWill);
                _mqttClient = null;
            }
        }

        private void VerifyConnection()
        {
            lock (verifyLock)
            {
                if (_mqttClient == null)
                {
                    LoggerHelper.Warn("MQTTClientManager::VerifyConnection() - MQTT Client NOT Connected. Connecting.");
                    Connect();
                    return;
                }

                if (lastVerifyConnection.AddSeconds(MINIMUM_DELAY_VERIFY_CONNECTION) >= DateTime.UtcNow)
                    return;

                lastVerifyConnection = DateTime.UtcNow;
                if (!_mqttClient.IsConnected())
                {
                    LoggerHelper.Warn("MQTTClientManager::VerifyConnection() - MQTT Connection Broken. Reconnecting.");

                    Disconnect(true);
                    Connect();
                }
            }
        }

        private void OnMonitorTimeElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                LoggerHelper.Debug("MQTTClientManager::OnMonitorTimeElapsed() - Checking MQTT Connection.");

                _monitorTimer.Enabled = false;
                VerifyConnection();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMonitorTimeElapsed() - Error: {0}", ex);
            }
            finally
            {
                _monitorTimer.Interval = MONITOR_TIMER_NORMAL_INTERVAL;
                _monitorTimer.Enabled = true;
            }
        }

        private void OnMQTTConnectedHandler(object sender, MQTTEventEventArgs e)
        {
            try
            {
                OnMQTTConnected?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTConnectedHandler() - Error: {0}", ex.ToString());
            }
        }

        private void OnMQTTDisconnectedHandler(object sender, MQTTEventEventArgs e)
        {
            try
            {
                OnMQTTDisconnected?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTDisconnectedHandler() - Error: {0}", ex.ToString());
            }
        }

        private void OnMQTTMessageReceivedHandler(object sender, MQTTMessageEventEventArgs e)
        {
            try
            {
                OnMQTTMessageReceived?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTMessageReceivedHandler() - Error: {0}", ex.ToString());
            }
        }

        private void OnMQTTRefreshMessageReceivedHandler(object sender, EventArgs e)
        {
            try
            {
                OnMQTTRefreshMessageReceived?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTRefreshMessageReceivedHandler() - Error: {0}", ex.ToString());
            }
        }
    }
}
