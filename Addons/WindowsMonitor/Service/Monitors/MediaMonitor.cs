using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
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

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Media/Volume",
                Value = Math.Round(PlatformHelper.GetAudioVolume(), 0)
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Media/Muted",
                Value = PlatformHelper.IsAudioMuted()
            });

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Media/Playing",
                Value = PlatformHelper.IsAudioPlaying()
            });

            return result;
        }
    }
}
