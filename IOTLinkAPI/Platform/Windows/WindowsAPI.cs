using AudioSwitcher.AudioApi.CoreAudio;
using IOTLink.Platform.Windows.Native;
using IOTLink.Platform.Windows.Native.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace IOTLink.Platform.Windows
{
#pragma warning disable 1591
    public static class WindowsAPI
    {
        private static readonly int WM_SYSCOMMAND = 0x0112;
        private static readonly uint SC_MONITORPOWER = 0xF170;
        private static readonly uint MOUSEEVENTFMOVE = 0x0001;

        public enum DialogStyle
        {
            // Buttons
            MB_OK = 0,
            MB_OKCANCEL = 1,
            MB_ABORTRETRYIGNORE = 2,
            MB_YESNOCANCEL = 3,
            MB_YESNO = 4,
            MB_RETRYCANCEL = 5,
            MB_CANCELTRYCONTINUE = 6,
            MB_HELP = 16384,
            // Icons
            MB_ICONERROR = 16,
            MB_ICONSTOP = 16,
            MB_ICONHAND = 16,
            MB_ICONQUESTION = 32,
            MB_ICONEXCLAMATION = 48,
            MB_ICONWARNING = 48,
            MB_ICONASTERISK = 64,
            MB_ICONINFORMATION = 64,
            MB_ICONMASK = 240
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
                    AdvApi32.DuplicateTokenEx(hImpersonationToken, 0, IntPtr.Zero, (int)SecurityImpersonationLevel.SecurityImpersonation, (int)TokenType.TokenPrimary, ref hUserToken))
                {

                    // Launch the child process interactively using the token of the logged user. 
                    ProcessInformation tProcessInfo;
                    StartupInfo tStartUpInfo = new StartupInfo();
                    tStartUpInfo.cb = StartupInfo.SizeOf;

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
            MemoryStatusEx memoryStatusEx = new MemoryStatusEx();
            if (Kernel32.GlobalMemoryStatusEx(memoryStatusEx))
            {
                MemoryInfo memoryInfo = new MemoryInfo
                {
                    MemoryLoad = memoryStatusEx.dwMemoryLoad,
                    AvailPhysical = (uint)Math.Round((decimal)memoryStatusEx.ullAvailPhys / MemoryInfo.MEMORY_DIVISOR, 0),
                    AvailVirtual = (uint)Math.Round((decimal)memoryStatusEx.ullAvailVirtual / MemoryInfo.MEMORY_DIVISOR),
                    AvailExtendedVirtual = (uint)Math.Round((decimal)memoryStatusEx.ullAvailExtendedVirtual / MemoryInfo.MEMORY_DIVISOR),
                    AvailPageFile = (uint)Math.Round((decimal)memoryStatusEx.ullAvailPageFile / MemoryInfo.MEMORY_DIVISOR),
                    TotalPhysical = (uint)Math.Round((decimal)memoryStatusEx.ullTotalPhys / MemoryInfo.MEMORY_DIVISOR),
                    TotalVirtual = (uint)Math.Round((decimal)memoryStatusEx.ullTotalVirtual / MemoryInfo.MEMORY_DIVISOR),
                    TotalPageFile = (uint)Math.Round((decimal)memoryStatusEx.ullTotalPageFile / MemoryInfo.MEMORY_DIVISOR),
                };

                return memoryInfo;
            }
            return null;
        }

        public static void ShowMessage(string title, string message)
        {
            uint sessionId = Kernel32.WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
                return;

            IntPtr server = GetServerPtr();
            try
            {
                int response = 0;
                int titleLen = title.Length * 2;
                int messageLen = message.Length * 2;
                int style = (int)(DialogStyle.MB_OK | DialogStyle.MB_ICONINFORMATION);

                WtsApi32.WTSSendMessage(server, (int)sessionId, title, titleLen, message, messageLen, style, 0, out response, false);
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
            }

        }

        public static bool SetAudioMute(bool mute)
        {
            CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            return defaultPlaybackDevice.Mute(mute);
        }

        public static bool ToggleAudioMute()
        {
            CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            return defaultPlaybackDevice.ToggleMute();
        }

        public static void SetAudioVolume(double volume)
        {
            if (volume < 0 || volume > 100)
                throw new Exception("Volume level needs to be between 0 and 100");

            CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            defaultPlaybackDevice.Volume = volume;
        }

        public static double GetAudioVolume()
        {
            CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            return defaultPlaybackDevice.Volume;
        }

        public static uint GetIdleTime()
        {
            uint idleTime = 0;

            LastInputInfo lastInputInfo = new LastInputInfo();
            lastInputInfo.cbSize = LastInputInfo.SizeOf;
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)(Environment.TickCount & int.MaxValue);

            if (User32.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime & int.MaxValue;
                idleTime = (envTicks - lastInputTick) & int.MaxValue;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        public static List<DisplayInfo> GetDisplays()
        {
            List<DisplayInfo> displays = new List<DisplayInfo>();
            User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
            {
                MonitorInfoEx mi = new MonitorInfoEx();
                mi.Size = MonitorInfoEx.SizeOf;
                if (User32.GetMonitorInfo(hMonitor, ref mi))
                {
                    DisplayInfo di = new DisplayInfo();
                    di.ScreenWidth = (mi.Monitor.Right - mi.Monitor.Left);
                    di.ScreenHeight = (mi.Monitor.Bottom - mi.Monitor.Top);
                    di.MonitorArea = mi.Monitor;
                    di.WorkArea = mi.WorkArea;
                    di.Availability = mi.Flags.ToString();
                    di.DeviceName = mi.DeviceName;
                    displays.Add(di);
                }
                return true;
            }, IntPtr.Zero);

            return displays;
        }

        private static List<int> GetSessionIDs(IntPtr server, bool activeOnly = false)
        {
            List<int> sessionIds = new List<int>();
            IntPtr buffer = IntPtr.Zero;
            int count = 0;
            int retval = WtsApi32.WTSEnumerateSessions(server, 0, 1, ref buffer, ref count);
            int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
            IntPtr current = buffer;

            if (retval != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
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
