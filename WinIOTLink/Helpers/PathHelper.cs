using System;

namespace WinIOTLink.Helpers
{
    public static class PathHelper
    {
        public static string BasePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string LogsPath()
        {
            return BasePath() + "\\Logs";
        }

        public static string DataPath()
        {
            return BasePath() + "\\Data";
        }

        public static string AddonsPath()
        {
            return BasePath() + "\\Addons";
        }
    }
}
