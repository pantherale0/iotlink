using IOTLinkAPI.Configs;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IOTLinkAddon.Service.Monitors
{
    class PowerMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Power";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration _config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            PowerStatus powerStatus = SystemInformation.PowerStatus;

            // Power Status
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Power/Status",
                Value = powerStatus.PowerLineStatus
            });

            // Battery Status
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/Status",
                Value = powerStatus.BatteryChargeStatus
            });

            // Battery Full Lifetime
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/FullLifetime",
                Value = powerStatus.BatteryFullLifetime
            });

            // Battery Remaining Time
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/RemainingTime",
                Value = powerStatus.BatteryLifeRemaining
            });

            // Battery Remaining (%)
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/RemainingPercent",
                Value = (powerStatus.BatteryLifePercent * 100)
            });

            return result;
        }
    }
}
