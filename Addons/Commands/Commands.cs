using Newtonsoft.Json;
using System;
using IOTLink.API;
using IOTLink.Engine.MQTT;
using IOTLink.Helpers;

namespace IOTLinkAddon
{
    public class Commands : AddonScript
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
            LoggerHelper.Debug("OnShutdownMessage message received");
            PlatformHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnRebootMessage message received");
            PlatformHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnLogoffMessage message received");
            PlatformHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnLockMessage message received");
            PlatformHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnHibernateMessage message received");
            PlatformHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnSuspendMessage message received");
            PlatformHelper.Suspend();
        }

        private void OnCommandRun(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnCommandRun message received");
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
                LoggerHelper.Error("OnCommandRun failure: {0}", ex.Message);
            }
        }
    }
}
