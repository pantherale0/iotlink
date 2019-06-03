using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WinIOTLink.Helpers.WinAPI
{
    public static class WindowsAPI
    {
        public enum WtsInfoClass
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

        public enum WtsConnectStateClass
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WtsSessionInfo
        {
            public Int32 SessionID;
            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;
            public WtsConnectStateClass State;
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

        public static void LockAll()
        {
            List<int> sessions = GetSessionIDs();
            Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions);
            foreach (KeyValuePair<string, int> entry in userSessionDictionary)
            {
                if (entry.Value != 0)
                    WTSDisconnectSession(IntPtr.Zero, entry.Value, true);
            }
        }

        public static bool LockUser(string username)
        {
            username = username.Trim().ToUpper();
            List<int> sessions = GetSessionIDs();
            Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions);
            if (userSessionDictionary.ContainsKey(username))
                return WTSDisconnectSession(IntPtr.Zero, userSessionDictionary[username], true);
            else
                return false;
        }

        public static void LogoffAll()
        {
            List<int> sessions = GetSessionIDs();
            Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions);
            foreach (KeyValuePair<string, int> entry in userSessionDictionary)
            {
                if (entry.Value != 0)
                    WTSLogoffSession(IntPtr.Zero, entry.Value, true);
            }
        }

        public static bool LogOffUser(string username)
        {
            username = username.Trim().ToUpper();
            List<int> sessions = GetSessionIDs();
            Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions);
            if (userSessionDictionary.ContainsKey(username))
                return WTSLogoffSession(IntPtr.Zero, userSessionDictionary[username], true);
            else
                return false;
        }

        public static List<int> GetSessionIDs()
        {
            List<int> sessionIds = new List<int>();
            IntPtr buffer = IntPtr.Zero;
            int count = 0;
            int retval = WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref buffer, ref count);
            int dataSize = Marshal.SizeOf(typeof(WtsSessionInfo));
            Int64 current = (int)buffer;

            if (retval != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    WtsSessionInfo si = (WtsSessionInfo)Marshal.PtrToStructure((IntPtr)current, typeof(WtsSessionInfo));
                    current += dataSize;
                    sessionIds.Add(si.SessionID);
                }
                WTSFreeMemory(buffer);
            }
            return sessionIds;
        }

        public static Dictionary<string, int> GetUserSessionDictionary(List<int> sessions)
        {
            Dictionary<string, int> userSession = new Dictionary<string, int>();

            foreach (var sessionId in sessions)
            {
                string uName = GetUsername(sessionId);
                if (!string.IsNullOrWhiteSpace(uName))
                    userSession.Add(uName, sessionId);
            }
            return userSession;
        }

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSLogoffSession(IntPtr hServer, int SessionId, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern Int32 WTSEnumerateSessions(IntPtr hServer, [MarshalAs(UnmanagedType.U4)] Int32 Reserved, [MarshalAs(UnmanagedType.U4)] Int32 Version, ref IntPtr ppSessionInfo, [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
    }
}
