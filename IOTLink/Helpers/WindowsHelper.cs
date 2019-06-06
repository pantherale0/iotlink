using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using IOTLink.Platform;
using IOTLink.Platform.Windows;

namespace IOTLink.Helpers
{
    public static class PlatformHelper
    {
        public static string GetFullMachineName()
        {
            string domainName = Environment.UserDomainName;
            string computerName = Environment.MachineName;
            if (domainName.Equals(computerName))
                return computerName;

            return string.Format("{0}\\{1}", domainName, computerName);
        }

        public static string GetUsername(int sessionId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return WindowsAPI.GetUsername(sessionId);

            throw new PlatformNotSupportedException();
        }

        public static void Shutdown(bool force = false)
        {
            LoggerHelper.Debug("Executing {0} system shutdown.", force ? "forced" : "normal");
            string filename = "shutdown";
            string args = null;

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                args = force ? "-s -f -t 0" : "-s -t 0";

            // Linux or OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                args = "-h now";

            Process.Start(filename, args);
        }

        public static void Reboot(bool force = false)
        {
            LoggerHelper.Debug("Executing {0} system shutdown.", force ? "forced" : "normal");
            string filename = "shutdown";
            string args = null;

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                args = force ? "-r -f -t 0" : "-r -t 0";

            // Linux or OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                args = "-r -h now";

            Process.Start(filename, args);
        }

        public static void Hibernate()
        {
            LoggerHelper.Debug("Executing system hibernation.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                WindowsAPI.Hibernate();

            throw new PlatformNotSupportedException();
        }

        public static void Suspend()
        {
            LoggerHelper.Debug("Executing system suspend.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                WindowsAPI.Suspend();

            throw new PlatformNotSupportedException();
        }

        public static void Logoff(string username)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            if (string.IsNullOrWhiteSpace(username))
            {
                LoggerHelper.Debug("Executing Logoff on all users");
                WindowsAPI.LogoffAll();
            }
            else
            {
                LoggerHelper.Debug(string.Format("Executing Logoff on user {0}", username));
                WindowsAPI.LogOffUser(username);
            }
        }

        public static void Lock(string username)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            if (string.IsNullOrWhiteSpace(username))
            {
                LoggerHelper.Debug("Locking all users sessions");
                WindowsAPI.LockAll();
            }
            else
            {
                LoggerHelper.Debug(string.Format("Locking {0} user session", username));
                WindowsAPI.LockUser(username);
            }
        }

        public static void Run(string command, string args, string path, string username)
        {
            if (!string.IsNullOrWhiteSpace(args))
                args = string.Format("{0} {1}", Path.GetFileName(command), args);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            LoggerHelper.Debug("Run - Command: {0} Args: {1} Path: {2} User: {3}", command, args, path, username);
            WindowsAPI.Run(command, args, path, username);
        }

        public static MemoryInfo GetMemoryInformation()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.GetMemoryInformation();
        }
    }
}
