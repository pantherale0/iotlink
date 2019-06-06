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
            LoggerHelper.Info("OnShutdownMessage message received");
            PlatformHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("OnRebootMessage message received");
            PlatformHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("OnLogoffMessage message received");
            PlatformHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("OnLockMessage message received");
            PlatformHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("OnHibernateMessage message received");
            PlatformHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("OnSuspendMessage message received");
            PlatformHelper.Suspend();
        }

        private void OnCommandRun(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("OnCommandRun message received");
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

                PlatformHelper.Run(command, args, path, user);
            }
            catch (Exception ex)
            {
                LoggerHelper.Info("OnCommandRun failure: {0}", ex.Message);
            }
        }
    }
}
