using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class MediaMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "MediaInfo";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            AudioDeviceInfo audioDeviceInfo = PlatformHelper.GetAudioDeviceInfo(Guid.Empty);
            if (audioDeviceInfo == null)
                return result;

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Media/Volume",
                Value = Math.Round(audioDeviceInfo.Volume, 0),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = "Volume",
                    Name = "Media Volume",
                    Component = HomeAssistantComponent.Sensor
                }
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Media/Muted",
                Value = audioDeviceInfo.IsMuted,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = "Muted",
                    Name = "Volume Muted",
                    Component = HomeAssistantComponent.BinarySensor,
                    PayloadOff = "False",
                    PayloadOn = "True"
                }
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Media/Playing",
                Value = audioDeviceInfo.IsAudioPlaying,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = "Playing",
                    Name = "Media Playing",
                    Component = HomeAssistantComponent.BinarySensor,
                    PayloadOff = "False",
                    PayloadOn = "True"
                }
            });

            return result;
        }
    }
}
