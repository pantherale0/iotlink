using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using WinIOTLink.Platform.Windows.Native;

namespace WinIOTLink.Platform.Windows
{
    public static class WindowsAPI
    {
        private const int MemoryDivisor = 1024 * 1024;
        public class MemoryInfo
        {
            public uint MemoryLoad { get; set; }
            public ulong TotalPhysical { get; set; }
            public ulong AvailPhysical { get; set; }
            public ulong TotalPageFile { get; set; }
            public ulong AvailPageFile { get; set; }
            public ulong TotalVirtual { get; set; }
            public ulong AvailVirtual { get; set; }
            public ulong AvailExtendedVirtual { get; set; }
        }

        public static string GetUsername(int sessionId)
        {
            IntPtr server = GetServerPtr();
            IntPtr buffer = IntPtr.Zero;
            string username = string.Empty;
            try
            {
                WtsApi32.WTSQuerySessionInformation(server, sessionId, WtsApi32.WtsInfoClass.WTSUserName, out buffer, out uint count);
                username = Marshal.PtrToStringAnsi(buffer).ToUpper().Trim();
            }
            finally
            {
                WtsApi32.WTSFreeMemory(buffer);
                WtsApi32.WTSCloseServer(server);
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

                WtsApi32.WTSQuerySessionInformation(server, sessionId, WtsApi32.WtsInfoClass.WTSDomainName, out buffer, out uint count);
                domain = Marshal.PtrToStringAnsi(buffer).ToUpper().Trim();
            }
            finally
            {
                WtsApi32.WTSFreeMemory(buffer);
                WtsApi32.WTSCloseServer(server);
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
                        WtsApi32.WTSDisconnectSession(server, entry.Value, true);
                    }
                }
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
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
                    return WtsApi32.WTSDisconnectSession(server, userSessionDictionary[username], true);
                else
                    return false;
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
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
                        WtsApi32.WTSLogoffSession(server, entry.Value, true);
                    }
                }
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
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
                    return WtsApi32.WTSLogoffSession(server, userSessionDictionary[username], true);
                else
                    return false;
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
            }
        }

        public static bool Hibernate()
        {
            return PowrProf.SetSuspendState(true, true, true);
        }

        public static bool Suspend()
        {
            return PowrProf.SetSuspendState(false, true, true);
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
                if (WtsApi32.WTSQueryUserToken(sessionId, out hImpersonationToken) &&
                    AdvApi32.DuplicateTokenEx(hImpersonationToken, 0, IntPtr.Zero, (int)AdvApi32.SecurityImpersonationLevel.SecurityImpersonation, (int)AdvApi32.TokenType.TokenPrimary, ref hUserToken))
                {

                    // Launch the child process interactively using the token of the logged user. 
                    AdvApi32.ProcessInformation tProcessInfo;
                    AdvApi32.StartupInfo tStartUpInfo = new AdvApi32.StartupInfo();
                    tStartUpInfo.cb = Marshal.SizeOf(typeof(AdvApi32.StartupInfo));

                    bool childProcStarted = AdvApi32.CreateProcessAsUser(
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
                        Kernel32.CloseHandle(tProcessInfo.hThread);
                        Kernel32.CloseHandle(tProcessInfo.hProcess);
                    }
                    Kernel32.CloseHandle(hImpersonationToken);
                    Kernel32.CloseHandle(hUserToken);

                    return childProcStarted;
                }
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
            }
            return false;
        }

        public static MemoryInfo GetMemoryInformation()
        {
            Kernel32.MemoryStatusEx memoryStatusEx = new Kernel32.MemoryStatusEx();
            if (Kernel32.GlobalMemoryStatusEx(memoryStatusEx))
            {
                MemoryInfo memoryInfo = new MemoryInfo
                {
                    MemoryLoad = memoryStatusEx.dwMemoryLoad,
                    AvailPhysical = (uint)Math.Round((decimal)memoryStatusEx.ullAvailPhys / MemoryDivisor, 0),
                    AvailVirtual = (uint)Math.Round((decimal)memoryStatusEx.ullAvailVirtual / MemoryDivisor),
                    AvailExtendedVirtual = (uint)Math.Round((decimal)memoryStatusEx.ullAvailExtendedVirtual / MemoryDivisor),
                    AvailPageFile = (uint)Math.Round((decimal)memoryStatusEx.ullAvailPageFile / MemoryDivisor),
                    TotalPhysical = (uint)Math.Round((decimal)memoryStatusEx.ullTotalPhys / MemoryDivisor),
                    TotalVirtual = (uint)Math.Round((decimal)memoryStatusEx.ullTotalVirtual / MemoryDivisor),
                    TotalPageFile = (uint)Math.Round((decimal)memoryStatusEx.ullTotalPageFile / MemoryDivisor),
                };

                return memoryInfo;
            }
            return null;
        }

        private static List<int> GetSessionIDs(IntPtr server, bool activeOnly = false)
        {
            List<int> sessionIds = new List<int>();
            IntPtr buffer = IntPtr.Zero;
            int count = 0;
            int retval = WtsApi32.WTSEnumerateSessions(server, 0, 1, ref buffer, ref count);
            int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
            Int64 current = (int)buffer;

            if (retval != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure((IntPtr)current, typeof(WtsApi32.WtsSessionInfo));
                    current += dataSize;
                    if (!activeOnly || si.State == WtsApi32.WtsConnectStateClass.WTSActive)
                        sessionIds.Add(si.SessionID);
                }
                WtsApi32.WTSFreeMemory(buffer);
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
            return WtsApi32.WTSOpenServer(Environment.MachineName);
        }
    }
}
