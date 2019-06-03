using System;
using System.Diagnostics;
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

        public static string GetUsername(int sessionId, bool prependDomain = false)
        {
            return WindowsAPI.GetUsername(sessionId, prependDomain);
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
                return;

            if (username.Trim().ToUpperInvariant().CompareTo("ALL") == 0)
                WindowsAPI.LogoffAll();
            else
                WindowsAPI.LogOffUser(username);
        }

        public static void Lock(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            if (username.Trim().ToUpperInvariant().CompareTo("ALL") == 0)
                WindowsAPI.LockAll();
            else
                WindowsAPI.LockUser(username);
        }
    }
}
