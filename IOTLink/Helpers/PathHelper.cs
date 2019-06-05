using System;
using System.IO;

namespace IOTLink.Helpers
{
    public static class PathHelper
    {
        public const string APP_FOLDER_NAME = "IOTLink";

        public static string BaseAppPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string BaseDataPath()
        {
            return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    APP_FOLDER_NAME
                );
        }

        public static string LogsPath()
        {
            return Path.Combine(BaseDataPath(), "Logs");
        }

        public static string DataPath()
        {
            return Path.Combine(BaseDataPath(), "Configs");
        }

        public static string AddonsPath()
        {
            return Path.Combine(BaseDataPath(), "Addons");
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
