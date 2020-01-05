using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Collections.Generic;
using System.IO;

namespace IOTLinkAddon.Service.Monitors
{
    class StorageMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "HardDrive";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo == null || !driveInfo.IsReady || driveInfo.DriveType != DriveType.Fixed)
                    continue;

                try
                {
                    string drive = driveInfo.Name.Remove(1, 2);
                    string topic = string.Format("Stats/HardDrive/{0}", drive);

                    long usedSpace = driveInfo.TotalSize - driveInfo.TotalFreeSpace;
                    int driveUsage = (int)((100.0 / driveInfo.TotalSize) * usedSpace);

                    // Total Size
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/TotalSize",
                        Value = driveInfo.TotalSize,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_TotalStorage",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Available Free Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/AvailableFreeSpace",
                        Value = driveInfo.AvailableFreeSpace,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_AvailableFreeSpace",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Total Free Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/TotalFreeSpace",
                        Value = driveInfo.TotalFreeSpace,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_TotalFreeSpace",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Used Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/UsedSpace",
                        Value = usedSpace,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_UsedSpace",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Drive Format
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/DriveFormat",
                        Value = driveInfo.DriveFormat,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_DriveFormat",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Drive Usage
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/DriveUsage",
                        Value = driveUsage,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_DriveUsage",
                            Unit = "%",
                            Icon = "mdi:chart-donut"
                        }
                    });

                    // Drive Label
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/VolumeLabel",
                        Value = driveInfo.VolumeLabel,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Name = $"{drive}_VolumeLabel",
                            Icon = "mdi:harddisk"
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                        LoggerHelper.Error("Access to drives not allowed. Error: {0}", ex.ToString());
                    else if (ex is DriveNotFoundException)
                        LoggerHelper.Error("Drive not found. Error: {0}", ex.ToString());
                    else if (ex is IOException)
                        LoggerHelper.Error("Drive inaccessible. Error: {0}", ex.ToString());
                    else
                        LoggerHelper.Error("Error while getting drive information: {0}", ex.ToString());
                }
            }
            return result;
        }
    }
}
