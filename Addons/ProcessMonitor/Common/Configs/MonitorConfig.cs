using IOTLinkAddon.Common.Helpers;
using IOTLinkAPI.Configs;
using System.Collections.Generic;
using System.Linq;

namespace IOTLinkAddon.Common.Configs
{
    public class MonitorConfig
    {
        public string Key { get; set; }
        public List<string> ProcessNames { get; set; }
        public List<string> ProcessWindows { get; set; }
        public List<string> ProcessHandles { get; set; }
        public MonitoringConfig Monitoring { get; set; }

        public static MonitorConfig FromConfiguration(Configuration configuration)
        {
            return new MonitorConfig
            {
                Key = configuration.Key.ToLowerInvariant(),
                ProcessNames = configuration.GetList<string>("processes").Select(x => ProcessHelper.CleanProcessName(x)).ToList(),
                ProcessWindows = configuration.GetList<string>("windows"),
                ProcessHandles = configuration.GetList<string>("handles"),
                Monitoring = MonitoringConfig.FromConfiguration(configuration.GetValue("configs"))
            };
        }
    }
}
