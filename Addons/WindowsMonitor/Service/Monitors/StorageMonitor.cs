using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
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

        public override List<MonitorItem> GetMonitorItems(Configuration _config, int interval)
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
                        Value = driveInfo.TotalSize
                    });

                    // Available Free Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/AvailableFreeSpace",
                        Value = driveInfo.AvailableFreeSpace
                    });

                    // Total Free Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/TotalFreeSpace",
                        Value = driveInfo.TotalFreeSpace
                    });

                    // Used Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/UsedSpace",
                        Value = usedSpace
                    });

                    // Drive Format
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/DriveFormat",
                        Value = driveInfo.DriveFormat
                    });

                    // Drive Usage
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/DriveUsage",
                        Value = driveUsage
                    });

                    // Drive Label
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/VolumeLabel",
                        Value = driveInfo.VolumeLabel
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
