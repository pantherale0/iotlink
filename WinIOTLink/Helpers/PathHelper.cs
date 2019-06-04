using System;
using System.IO;

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

        public static string GetFileText(string path)
        {
            if (path == null || !File.Exists(path))
                return null;

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var textReader = new StreamReader(fileStream))
                {
                    return textReader.ReadToEnd();
                }
            }
        }
    }
}
