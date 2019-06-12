using IOTLink.Addons;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events.MQTT;
using Newtonsoft.Json;
using System;

namespace IOTLinkAddon
{
    public class Commands : AddonScript
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            _manager.SubscribeTopic(this, "shutdown", OnShutdownMessage);
            _manager.SubscribeTopic(this, "reboot", OnRebootMessage);
            _manager.SubscribeTopic(this, "logoff", OnLogoffMessage);
            _manager.SubscribeTopic(this, "lock", OnLockMessage);
            _manager.SubscribeTopic(this, "hibernate", OnHibernateMessage);
            _manager.SubscribeTopic(this, "suspend", OnSuspendMessage);
            _manager.SubscribeTopic(this, "run", OnRunMessage);
            _manager.SubscribeTopic(this, "volume/set", OnVolumeSetMessage);
            _manager.SubscribeTopic(this, "volume/mute", OnVolumeMuteMessage);
        }

        private void OnShutdownMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnShutdownMessage: Message received");
            PlatformHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnRebootMessage: Message received");
            PlatformHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnLogoffMessage: Message received");
            PlatformHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnLockMessage: Message received");
            PlatformHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnHibernateMessage: Message received");
            PlatformHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnSuspendMessage: Message received");
            PlatformHelper.Suspend();
        }

        private void OnRunMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnCommandRun: Message received");
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

        private void OnVolumeSetMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnVolumeSet: Message received");
            try
            {
                double volume = Convert.ToDouble(e.Message.GetPayload());
                PlatformHelper.SetAudioVolume(volume);

                LoggerHelper.Debug("OnVolumeSet: Volume set to {0}", volume);
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnVolumeSet: Wrong Payload: {0}", ex.Message);
            }
        }

        private void OnVolumeMuteMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnVolumeMute: Message received");
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message.GetPayload()))
                {
                    PlatformHelper.ToggleAudioMute();
                    LoggerHelper.Debug("OnVolumeMute: Toggling current audio mute flag.");
                    return;
                }

                bool mute = Convert.ToBoolean(e.Message.GetPayload());
                PlatformHelper.SetAudioMute(mute);

                LoggerHelper.Debug("OnVolumeMute: Mute flag set to {0}", mute);
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnVolumeMute: Wrong Payload: {0}", ex.Message);
            }
        }
    }
}
