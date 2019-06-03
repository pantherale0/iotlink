using System;
using System.Collections.Generic;
using System.Linq;
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

        private enum SecurityImpersonationLevel
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        private enum TokenType
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WtsSessionInfo
        {
            public Int32 SessionID;
            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;
            public WtsConnectStateClass State;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct StartupInfo
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public static string GetUsername(int sessionId)
        {
            IntPtr server = GetServerPtr();
            IntPtr buffer = IntPtr.Zero;
            string username = string.Empty;
            try
            {
                WTSQuerySessionInformation(server, sessionId, WtsInfoClass.WTSUserName, out buffer, out uint count);
                username = Marshal.PtrToStringAnsi(buffer).ToUpper().Trim();
            }
            finally
            {
                WTSFreeMemory(buffer);
                WTSCloseServer(server);
            }
            return username;
        }

        public static string GetDomainName(int sessionId)
        {
            IntPtr server = GetServerPtr();
            IntPtr buffer = IntPtr.Zero;
            string domain = string.Empty;
            try
            {

                WTSQuerySessionInformation(server, sessionId, WtsInfoClass.WTSDomainName, out buffer, out uint count);
                domain = Marshal.PtrToStringAnsi(buffer).ToUpper().Trim();
            }
            finally
            {
                WTSFreeMemory(buffer);
                WTSCloseServer(server);
            }
            return domain;
        }

        public static void LockAll()
        {
            IntPtr server = GetServerPtr();
            try
            {
                List<int> sessions = GetSessionIDs(server);
                Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions, server);
                foreach (KeyValuePair<string, int> entry in userSessionDictionary)
                {
                    if (entry.Value != 0)
                    {
                        WTSDisconnectSession(server, entry.Value, true);
                    }
                }
            }
            finally
            {
                WTSCloseServer(server);
            }
        }

        public static bool LockUser(string username)
        {
            IntPtr server = GetServerPtr();
            try
            {
                username = username.Trim().ToUpper();

                List<int> sessions = GetSessionIDs(server);
                Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions, server);

                if (userSessionDictionary.ContainsKey(username))
                    return WTSDisconnectSession(server, userSessionDictionary[username], true);
                else
                    return false;
            }
            finally
            {
                WTSCloseServer(server);
            }
        }

        public static void LogoffAll()
        {
            IntPtr server = GetServerPtr();
            try
            {
                List<int> sessions = GetSessionIDs(server);
                Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions, server);
                foreach (KeyValuePair<string, int> entry in userSessionDictionary)
                {
                    if (entry.Value != 0)
                    {
                        WTSLogoffSession(server, entry.Value, true);
                    }
                }
            }
            finally
            {
                WTSCloseServer(server);
            }
        }

        public static bool LogOffUser(string username)
        {
            IntPtr server = GetServerPtr();
            try
            {
                username = username.Trim().ToUpper();

                List<int> sessions = GetSessionIDs(server);
                Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions, server);
                if (userSessionDictionary.ContainsKey(username))
                    return WTSLogoffSession(server, userSessionDictionary[username], true);
                else
                    return false;
            }
            finally
            {
                WTSCloseServer(server);
            }
        }

        public static bool Run(string command, string args = null, string workDir = null, string username = null)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;
            if (string.IsNullOrWhiteSpace(args))
                args = null;
            if (string.IsNullOrWhiteSpace(workDir))
                workDir = null;

            IntPtr server = GetServerPtr();
            try
            {

                List<int> sessions = GetSessionIDs(server, true);
                Dictionary<string, int> userSessionDictionary = GetUserSessionDictionary(sessions, server);
                if (userSessionDictionary.Keys.Count == 0)
                    return false;

                int sessionId = userSessionDictionary.First().Value;
                if (!string.IsNullOrWhiteSpace(username))
                {
                    username = username.Trim().ToUpper();
                    if (!userSessionDictionary.ContainsKey(username))
                        return false;

                    sessionId = userSessionDictionary[username];
                }

                IntPtr hImpersonationToken = IntPtr.Zero;
                IntPtr hUserToken = IntPtr.Zero;
                if (WTSQueryUserToken(sessionId, out hImpersonationToken) &&
                    DuplicateTokenEx(hImpersonationToken, 0, IntPtr.Zero, (int)SecurityImpersonationLevel.SecurityImpersonation, (int)TokenType.TokenPrimary, ref hUserToken))
                {

                    // Launch the child process interactively using the token of the logged user. 
                    ProcessInformation tProcessInfo;
                    StartupInfo tStartUpInfo = new StartupInfo();
                    tStartUpInfo.cb = Marshal.SizeOf(typeof(StartupInfo));

                    bool childProcStarted = CreateProcessAsUser(
                                hUserToken,         // Token of the logged-on user. 
                                command,            // Name of the process to be started. 
                                args,               // Any command line arguments to be passed. 
                                IntPtr.Zero,        // Default Process' attributes. 
                                IntPtr.Zero,        // Default Thread's attributes. 
                                false,              // Does NOT inherit parent's handles. 
                                0,                  // No any specific creation flag. 
                                IntPtr.Zero,        // Default environment path. 
                                workDir,            // Default current directory. 
                                ref tStartUpInfo,   // Process Startup Info.  
                                out tProcessInfo    // Process information to be returned. 
                                );

                    if (childProcStarted)
                    {
                        // If the child process is created, it can be controlled via the out  
                        // param "tProcessInfo". For now, as we don't want to do any thing  
                        // with the child process, closing the child process' handles  
                        // to prevent the handle leak. 
                        CloseHandle(tProcessInfo.hThread);
                        CloseHandle(tProcessInfo.hProcess);
                    }
                    CloseHandle(hImpersonationToken);
                    CloseHandle(hUserToken);

                    return childProcStarted;
                }
            }
            finally
            {
                WTSCloseServer(server);
            }
            return false;
        }

        private static List<int> GetSessionIDs(IntPtr server, bool activeOnly = false)
        {
            List<int> sessionIds = new List<int>();
            IntPtr buffer = IntPtr.Zero;
            int count = 0;
            int retval = WTSEnumerateSessions(server, 0, 1, ref buffer, ref count);
            int dataSize = Marshal.SizeOf(typeof(WtsSessionInfo));
            Int64 current = (int)buffer;

            if (retval != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    WtsSessionInfo si = (WtsSessionInfo)Marshal.PtrToStructure((IntPtr)current, typeof(WtsSessionInfo));
                    current += dataSize;
                    if (!activeOnly || si.State == WtsConnectStateClass.WTSActive)
                        sessionIds.Add(si.SessionID);
                }
                WTSFreeMemory(buffer);
            }
            return sessionIds;
        }

        private static Dictionary<string, int> GetUserSessionDictionary(List<int> sessions, IntPtr server)
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

        private static IntPtr GetServerPtr()
        {
            return WTSOpenServer(Environment.MachineName);
        }

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSLogoffSession(IntPtr hServer, int SessionId, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern Int32 WTSEnumerateSessions(IntPtr hServer, [MarshalAs(UnmanagedType.U4)] Int32 Reserved, [MarshalAs(UnmanagedType.U4)] Int32 Version, ref IntPtr ppSessionInfo, [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool WTSQueryUserToken(Int32 sessionId, out IntPtr Token);

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        private static extern bool DuplicateTokenEx(
            IntPtr ExistingTokenHandle,
            uint dwDesiredAccess,
            IntPtr lpThreadAttributes,
            int TokenType,
            int ImpersonationLevel,
            ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation
            );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CloseHandle(IntPtr hHandle);
    }
}
