using Newtonsoft.Json;
using System;
using IOTLink.API;
using IOTLink.Engine.MQTT;
using IOTLink.Helpers;

namespace IOTLink.Addons
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
            LoggerHelper.Info(typeof(Windows), "OnShutdownMessage message received");
            WindowsHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info(typeof(Windows), "OnRebootMessage message received");
            WindowsHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info(typeof(Windows), "OnLogoffMessage message received");
            WindowsHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info(typeof(Windows), "OnLockMessage message received");
            WindowsHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info(typeof(Windows), "OnHibernateMessage message received");
            WindowsHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info(typeof(Windows), "OnSuspendMessage message received");
            WindowsHelper.Suspend();
        }

        private void OnCommandRun(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info(typeof(Windows), "OnCommandRun message received");
            string value = e.Message.GetPayload();
            if (value == null)
                return;

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);
                string command = json.command;
                string args = json.args;
                string path = json.path;
                string user = json.user;

                WindowsHelper.Run(command, args, path, user);
            }
            catch (Exception ex)
            {
                LoggerHelper.Info(typeof(Windows), "OnCommandRun failure: {0}", ex.Message);
            }
        }
    }
}
