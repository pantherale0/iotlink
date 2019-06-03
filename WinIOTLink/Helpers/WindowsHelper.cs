using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public static void Logoff()
        {
            ExitWindowsEx(0, 0);
        }

        public static void Lock()
        {
            LockWorkStation();
        }

        public static void Hibernate()
        {
            SetSuspendState(true, true, true);
        }

        public static void Suspend()
        {
            SetSuspendState(false, true, true);
        }

        public static string GetUsername(int sessionId, bool prependDomain = false)
        {
            IntPtr buffer;
            int strLen;
            string username = "SYSTEM";
            if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out buffer, out strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer);
                WTSFreeMemory(buffer);
                if (prependDomain)
                {
                    if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSDomainName, out buffer, out strLen) && strLen > 1)
                    {
                        username = Marshal.PtrToStringAnsi(buffer) + "\\" + username;
                        WTSFreeMemory(buffer);
                    }
                }
            }
            return username;
        }

        private enum WtsInfoClass
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
        }

        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out System.IntPtr ppBuffer, out int pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32")]
        private static extern void LockWorkStation();

        [DllImport("user32")]
        private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
    }
}
