using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events.MQTT;
using Newtonsoft.Json;
using System;

namespace IOTLinkAddon
{
    public class Commands : ServiceAddon
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            GetManager().SubscribeTopic(this, "shutdown", OnShutdownMessage);
            GetManager().SubscribeTopic(this, "reboot", OnRebootMessage);
            GetManager().SubscribeTopic(this, "logoff", OnLogoffMessage);
            GetManager().SubscribeTopic(this, "lock", OnLockMessage);
            GetManager().SubscribeTopic(this, "hibernate", OnHibernateMessage);
            GetManager().SubscribeTopic(this, "suspend", OnSuspendMessage);
            GetManager().SubscribeTopic(this, "run", OnRunMessage);
            GetManager().SubscribeTopic(this, "volume/set", OnVolumeSetMessage);
            GetManager().SubscribeTopic(this, "volume/mute", OnVolumeMuteMessage);
            GetManager().SubscribeTopic(this, "notify", OnNotifyMessage);
        }

        private void OnShutdownMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnShutdownMessage: Message received");
            PlatformHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnRebootMessage: Message received");
            PlatformHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnLogoffMessage: Message received");
            PlatformHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnLockMessage: Message received");
            PlatformHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnHibernateMessage: Message received");
            PlatformHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnSuspendMessage: Message received");
            PlatformHelper.Suspend();
        }

        private void OnRunMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnCommandRun: Message received");
            string value = e.Message.GetPayload();
            if (value == null)
                return;

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);

                RunInfo runInfo = new RunInfo
                {
                    Application = json.command,
                    CommandLine = json.args,
                    WorkingDir = json.path,
                    Username = json.user,
                    Visible = json.visible,
                    Fallback = json.fallback
                };

                PlatformHelper.Run(runInfo);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnCommandRun failure: {0}", ex.Message);
            }
        }

        private void OnVolumeSetMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnVolumeSet: Message received");
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
            LoggerHelper.Verbose("OnVolumeMute: Message received");
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

        private void OnNotifyMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnNotifyMessage: Message received");
            string value = e.Message.GetPayload();
            if (value == null)
                return;

            try
            {
                dynamic json = JsonConvert.DeserializeObject(value);
                GetManager().ShowNotification(this, (string)json.title, (string)json.message, (string)json.iconUrl);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnNotifyMessage failure: {0}", ex.Message);
            }
        }
    }
}
