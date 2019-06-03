using System;
using System.Diagnostics;
using System.IO;
using WinIOTLink.Helpers.WinAPI;

namespace WinIOTLink.Helpers
{
    public static class WindowsHelper
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
            return WindowsAPI.GetUsername(sessionId);
        }

        public static void Shutdown(bool force = false)
        {
            if (force)
                Process.Start("shutdown", "/s /f /t 0");
            else
                Process.Start("shutdown", "/s /t 0");
        }

        public static void Reboot(bool force = false)
        {
            if (force)
                Process.Start("shutdown", "/r /f /t 0");
            else
                Process.Start("shutdown", "/r /t 0");
        }

        public static void Hibernate()
        {
            WindowsAPI.SetSuspendState(true, true, true);
        }

        public static void Suspend()
        {
            WindowsAPI.SetSuspendState(false, true, true);
        }

        public static void Logoff(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                LoggerHelper.Debug("WindowsHelper", "Executing logoff on all users");
                WindowsAPI.LogoffAll();
            }
            else
            {
                LoggerHelper.Debug("WindowsHelper", string.Format("Executing Logoff on user {0}", username));
                WindowsAPI.LogOffUser(username);
            }
        }

        public static void Lock(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                LoggerHelper.Debug("WindowsHelper", "Locking all users sessions");
                WindowsAPI.LockAll();
            }
            else
            {
                LoggerHelper.Debug("WindowsHelper", string.Format("Locking {0} user session", username));
                WindowsAPI.LockUser(username);
            }
        }

        public static void Run(string command, string args, string path, string username)
        {
            if (!string.IsNullOrWhiteSpace(args))
                args = string.Format("{0} {1}", Path.GetFileName(command), args);

            LoggerHelper.Info("WindowsHelper", String.Format("Run - Command: {0} Args: {1} Path: {2} User: {3}", command, args, path, username));
            WindowsAPI.Run(command, args, path, username);
        }
    }
}
