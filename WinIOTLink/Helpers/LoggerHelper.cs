using System;
using System.IO;

namespace WinIOTLink.Helpers
{
    public static class LoggerHelper
    {
        public enum LogLevel
        {
            DISABLED,
            CRITICAL,
            ERROR,
            WARNING,
            INFO,
            DEBUG,
            TRACE,
            HELP_ME
        }

        public static void Critical(string messageTag, string message)
        {
            WriteToFile(messageTag, message, LogLevel.CRITICAL);
        }

        public static void Error(string messageTag, string message)
        {
            WriteToFile(messageTag, message, LogLevel.ERROR);
        }

        public static void Warn(string messageTag, string message)
        {
            WriteToFile(messageTag, message, LogLevel.WARNING);
        }

        public static void Info(string messageTag, string message)
        {
            WriteToFile(messageTag, message, LogLevel.INFO);
        }

        public static void Debug(string messageTag, string message)
        {
            WriteToFile(messageTag, message, LogLevel.DEBUG);
        }

        public static void Trace(string messageTag, string message)
        {
            WriteToFile(messageTag, message, LogLevel.TRACE);
        }

        public static void WriteToFile(string messageTag, string message, LogLevel logLevel)
        {
            string finalMessage = String.Format("[{0}][{1}][{2}][{3}]: {4}", WindowsHelper.GetFullMachineName(), DateTime.Now, logLevel.ToString(), messageTag, message);

            string logsPath = PathHelper.LogsPath();
            if (!Directory.Exists(logsPath))
                Directory.CreateDirectory(logsPath);

            string filepath = logsPath + "\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".log";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(finalMessage);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(finalMessage);
                }
            }
        }
    }
}
