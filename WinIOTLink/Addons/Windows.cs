﻿using Newtonsoft.Json;
using System;
using WinIOTLink.API;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Helpers;

namespace WinIOTLink.Addons
{
    internal class Windows : AddonScript
    {
        public override void Init()
        {
            base.Init();

            _manager.SubscribeTopic(this, "shutdown", OnShutdownMessage);
            _manager.SubscribeTopic(this, "reboot", OnRebootMessage);
            _manager.SubscribeTopic(this, "logoff", OnLogoffMessage);
            _manager.SubscribeTopic(this, "lock", OnLockMessage);
            _manager.SubscribeTopic(this, "hibernate", OnHibernateMessage);
            _manager.SubscribeTopic(this, "suspend", OnSuspendMessage);
            _manager.SubscribeTopic(this, "run", OnCommandRun);
        }

        private void OnShutdownMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnShutdownMessage message received");
            WindowsHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnRebootMessage message received");
            WindowsHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnLogoffMessage message received");
            WindowsHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnLockMessage message received");
            WindowsHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnHibernateMessage message received");
            WindowsHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnSuspendMessage message received");
            WindowsHelper.Suspend();
        }

        private void OnCommandRun(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnCommandRun message received");
            string value = e.Message.GetPayload();
            if (value == null)
                return;

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);
                var command = json.command;
                var args = json.args;
                var path = json.path;

                LoggerHelper.Info("Windows", String.Format("Command: {0} Args: {1} Path: {2}", command, args, path));
            }
            catch (Exception ex)
            {
                LoggerHelper.Info("Windows", "OnCommandRun failure: " + ex.Message);
            }
        }
    }
}
