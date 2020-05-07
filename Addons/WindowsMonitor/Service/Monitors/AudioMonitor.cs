using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.HomeAssistant;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
                CreateAudioMonitorItem(result, device, topic, type);

                if (device.IsDefaultDevice)
                {
                    topic = string.Format("Stats/Audio/Devices/{0}/Default/", type);
                    CreateAudioMonitorItem(result, device, topic, string.Format("Default {0}", type));
                }

                if (device.IsDefaultCommunicationsDevice)
                {
                    topic = string.Format("Stats/Audio/Devices/{0}/DefaultComms/", type);
                    CreateAudioMonitorItem(result, device, topic, string.Format("Default Comms {0}", type));
                }
            }

            Dictionary<string, string> deviceNames = devices.ToDictionary(x => x.Guid.ToString(), x => x.Name);
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Audio/Devices",
                Value = JsonConvert.SerializeObject(deviceNames),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = "Audio_Devices",
                    Name = "Audio Devices",
                    Component = HomeAssistantComponent.Sensor
                }
            });

            return result;
        }

        private static void CreateAudioMonitorItem(List<MonitorItem> result, AudioDeviceInfo device, string topic, string prefix)
        {
            string prefixId = prefix.Replace(" ", "_").Trim(new Char[] { '[', ']', '(', ')' });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = topic + "Name",
                Value = device.Name,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = string.Format("{0}_{1}_Name", prefixId, device.Guid),
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
                    Id = string.Format("{0}_{1}_Volume", prefixId, device.Guid),
                    Name = string.Format("{0} {1} Volume", prefix, device.Name),
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
                    Id = string.Format("{0}_{1}_Volume_Muted", prefixId, device.Guid),
                    Name = string.Format("{0} {1} Volume Muted", prefix, device.Name),
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
                    Id = string.Format("{0}_{1}_Media_Playing", prefixId, device.Guid),
                    Name = string.Format("{0} {1} Media Playing", prefix, device.Name),
                    Component = HomeAssistantComponent.BinarySensor,
                    DeviceClass = "sound",
                    PayloadOff = "False",
                    PayloadOn = "True"
                }
            });
        }
    }
}
