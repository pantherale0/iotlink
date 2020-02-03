using IOTLinkAddon.Common;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events.MQTT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace IOTLinkAddon.Service
{
    public class CommandsService : ServiceAddon
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

            GetManager().SubscribeTopic(this, "displays/on", OnDisplayTurnOnMessage);
            GetManager().SubscribeTopic(this, "displays/off", OnDisplayTurnOffMessage);

            GetManager().SubscribeTopic(this, "audio/volume", OnAudioVolumeSetMessage);
            GetManager().SubscribeTopic(this, "audio/mute", OnAudioMuteMessage);
            GetManager().SubscribeTopic(this, "audio/default", OnAudioSetDefaultMessage);
            GetManager().SubscribeTopic(this, "audio/default-comms", OnAudioSetDefaultCommsMessage);

            GetManager().SubscribeTopic(this, "notify", OnNotifyMessage);
            GetManager().SubscribeTopic(this, "sendKeys", OnSendKeysMessage);

            GetManager().SubscribeTopic(this, "media/playpause", OnMediaPlayPauseMessage);
            GetManager().SubscribeTopic(this, "media/stop", OnMediaStopMessage);
            GetManager().SubscribeTopic(this, "media/next", OnMediaNextMessage);
            GetManager().SubscribeTopic(this, "media/previous", OnMediaPreviousMessage);
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
            LoggerHelper.Verbose("OnRunMessage: Message received");
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
                LoggerHelper.Error("OnRunMessage failure: {0}", ex.Message);
            }
        }

        private void OnAudioVolumeSetMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnAudioVolumeSetMessage: Message received");
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message.GetPayload()))
                {
                    LoggerHelper.Warn("OnAudioVolumeSetMessage: Received an empty message payload");
                    return;
                }

                string[] args = e.Message.GetPayload().Split(',');
                double volume = Convert.ToDouble(args[args.Length == 2 ? 1 : 0]);
                Guid guid = args.Length >= 2 ? Guid.Parse(args[0]) : Guid.Empty;

                PlatformHelper.SetAudioVolume(guid, volume);
                LoggerHelper.Debug("OnAudioVolumeSetMessage: Volume set to {0}", volume);
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnAudioVolumeSetMessage: Wrong Payload: {0}", ex.Message);
            }
        }

        private void OnAudioMuteMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnAudioMuteMessage: Message received");
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message.GetPayload()))
                {
                    PlatformHelper.ToggleAudioMute(Guid.Empty);
                    LoggerHelper.Debug("OnAudioMuteMessage: Toggling current audio mute flag.");
                    return;
                }

                string[] args = e.Message.GetPayload().Split(',');
                bool mute = Convert.ToBoolean(args[args.Length == 2 ? 1 : 0]);
                Guid guid = args.Length >= 2 ? Guid.Parse(args[0]) : Guid.Empty;

                PlatformHelper.SetAudioMute(guid, mute);
                LoggerHelper.Debug("OnAudioMuteMessage: Mute flag set to {0}", mute);
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnAudioMuteMessage: Wrong Payload: {0}", ex.Message);
            }
        }

        private void OnAudioSetDefaultMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnAudioSetDefaultMessage: Message received");
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message.GetPayload()))
                {
                    LoggerHelper.Warn("OnAudioSetDefaultMessage: Received an empty message payload");
                    return;
                }

                Guid guid = Guid.Parse(e.Message.GetPayload());

                PlatformHelper.SetAudioDefault(guid);
                LoggerHelper.Debug("OnAudioVolumeSetMessage: Set Audio Device {0} to Default", guid);
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnAudioSetDefaultMessage: Wrong Payload: {0}", ex.Message);
            }
        }

        private void OnAudioSetDefaultCommsMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnAudioSetDefaultCommsMessage: Message received");
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message.GetPayload()))
                {
                    LoggerHelper.Warn("OnAudioSetDefaultCommsMessage: Received an empty message payload");
                    return;
                }

                Guid guid = Guid.Parse(e.Message.GetPayload());

                PlatformHelper.SetAudioDefaultComms(guid);
                LoggerHelper.Debug("OnAudioSetDefaultCommsMessage: Set Audio Device {0} to Default Comms", guid);
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("OnAudioSetDefaultCommsMessage: Wrong Payload: {0}", ex.Message);
            }
        }

        private void OnDisplayTurnOnMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnDisplayTurnOnMessage: Message received");

            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_TURN_ON;
            GetManager().SendAgentRequest(this, addonData);
        }

        private void OnDisplayTurnOffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("OnDisplayTurnOffMessage: Message received");

            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_TURN_OFF;
            GetManager().SendAgentRequest(this, addonData);
        }

        private void OnMediaPlayPauseMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnMediaPlayPause: Message received");
            RequestPressKey(0xB3);
        }

        private void OnMediaStopMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnMediaStopMessage: Message received");
            RequestPressKey(0xB2);
        }

        private void OnMediaNextMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnMediaNextMessage: Message received");
            RequestPressKey(0xB0);
        }

        private void OnMediaPreviousMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnMediaPreviousMessage: Message received");
            RequestPressKey(0xB1);
        }

        private void OnSendKeysMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Verbose("OnSendKeysMessage: Message received");
            string payload = e.Message.GetPayload();
            if (string.IsNullOrWhiteSpace(payload))
                return;

            try
            {
                dynamic data;
                if (payload.StartsWith("[") && payload.EndsWith("]"))
                    data = JsonConvert.DeserializeObject<List<string>>(payload);
                else
                    data = new List<string> { payload };

                dynamic addonData = new ExpandoObject();
                addonData.requestType = AddonRequestType.REQUEST_KEYS_SEND;
                addonData.requestData = data;

                GetManager().SendAgentRequest(this, addonData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnSendKeysMessage failure: {0}", ex.Message);
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
                GetManager().ShowNotification(this, (string)json.title, (string)json.message, (string)json.iconUrl, (string)json.launchParams);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnNotifyMessage failure: {0}", ex.Message);
            }
        }

        private void RequestPressKey(byte keyCode)
        {
            LoggerHelper.Verbose("PressKey: {0}", keyCode);

            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_KEYS_PRESS;
            addonData.requestData = keyCode;

            GetManager().SendAgentRequest(this, addonData);
        }
    }
}
