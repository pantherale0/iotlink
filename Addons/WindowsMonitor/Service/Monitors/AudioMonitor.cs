using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.HomeAssistant;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class AudioMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "AudioInfo";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            List<AudioDeviceInfo> devices = PlatformHelper.GetAudioDevices();
            foreach (AudioDeviceInfo device in devices)
            {
                string type = device.IsCaptureDevice ? "Input" : "Output";
                string topic = string.Format("Stats/Audio/Devices/{0}/{1}/", type, device.Guid);
                CreateAudioMonitorItem(result, device, topic);

                if (device.IsDefaultDevice)
                {
                    topic = string.Format("Stats/Audio/Devices/{0}/Default/", type);
                    CreateAudioMonitorItem(result, device, topic);
                }

                if (device.IsDefaultCommunicationsDevice)
                {
                    topic = string.Format("Stats/Audio/Devices/{0}/DefaultComms/", type);
                    CreateAudioMonitorItem(result, device, topic);
                }
            }

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Audio/Devices",
                Value = JsonConvert.SerializeObject(devices),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = "Audio_Devices",
                    Name = "Audio Devices",
                    Component = HomeAssistantComponent.Sensor
                }
            });

            return result;
        }

        private static void CreateAudioMonitorItem(List<MonitorItem> result, AudioDeviceInfo device, string topic)
        {
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = topic + "Name",
                Value = device.Name,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = string.Format("{0}_Name", device.Guid),
                    Name = device.Name,
                    Component = HomeAssistantComponent.Sensor
                }
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = topic + "Volume",
                Value = Math.Round(device.Volume, 0),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = string.Format("{0}_Volume", device.Guid),
                    Name = string.Format("{0} Volume", device.Name),
                    Component = HomeAssistantComponent.Sensor,
                    Icon = "mdi:volume"
                }
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = topic + "Muted",
                Value = device.IsMuted,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = string.Format("{0}_Volume_Muted", device.Guid),
                    Name = string.Format("{0} Volume Muted", device.Name),
                    Component = HomeAssistantComponent.BinarySensor,
                    DeviceClass = "sound",
                    PayloadOff = "False",
                    PayloadOn = "True"
                }
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = topic + "Playing",
                Value = device.IsAudioPlaying,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = string.Format("{0}_Media_Playing", device.Guid),
                    Name = string.Format("{0} Media Playing", device.Name),
                    Component = HomeAssistantComponent.BinarySensor,
                    DeviceClass = "sound",
                    PayloadOff = "False",
                    PayloadOn = "True"
                }
            });
        }
    }
}
