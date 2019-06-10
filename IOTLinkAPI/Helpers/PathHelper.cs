using System;
using System.IO;

namespace IOTLink.Helpers
{
    public static class PathHelper
    {
        public const string APP_FOLDER_NAME = "IOTLink";

        /// <summary>
        /// Return the application full name (including path)
        /// </summary>
        /// <returns>String</returns>
        public static string BaseAppFullName()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().Location;
        }

        /// <summary>
        /// Return the base application path (where the application executable is).
        /// </summary>
        /// <returns>String</returns>
        public static string BaseAppPath()
        {
            return Path.GetDirectoryName(BaseAppFullName());
        }

        /// <summary>
        /// Return the application name
        /// </summary>
        /// <returns>String</returns>
        public static string BaseAppName()
        {
            return Path.GetFileName(BaseAppFullName());
        }

        /// <summary>
        /// Return the base data path (configuration, addons, logs, etc).
        /// </summary>
        /// <returns>String</returns>
        public static string BaseDataPath()
        {
            return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    APP_FOLDER_NAME
                );
        }

        /// <summary>
        /// Logs Path
        /// </summary>
        /// <returns>String</returns>
        public static string LogsPath()
        {
            return Path.Combine(BaseDataPath(), "Logs");
        }

        /// <summary>
        /// Configuration Path
        /// </summary>
        /// <returns>String</returns>
        public static string ConfigPath()
        {
            return Path.Combine(BaseDataPath(), "Configs");
        }

        /// <summary>
        /// Addons Path
        /// </summary>
        /// <returns>String</returns>
        public static string AddonsPath()
        {
            return Path.Combine(BaseDataPath(), "Addons");
        }

        /// <summary>
        /// Read a shared text file from the system
        /// </summary>
        /// <param name="path">String containing the file path</param>
        /// <returns>String containing the file contents</returns>
        public static string GetFileText(string path)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }
    }
}
