using AudioSwitcher.AudioApi.CoreAudio;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Windows.Native;
using IOTLinkAPI.Platform.Windows.Native.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows
{
#pragma warning disable 1591
    public static class WindowsAPI
    {
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MOUSEEVENTFMOVE = 0x0001;

        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int CREATE_NO_WINDOW = 0x08000000;
        private const int CREATE_NEW_CONSOLE = 0x00000010;

        private const uint INVALID_SESSION_ID = 0xFFFFFFFF;

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
                username = Marshal.PtrToStringAnsi(buffer).ToLowerInvariant().Trim();
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
                List<WindowsSessionInfo> sessionInfos = GetWindowsSessions(server);
                foreach (var sessionInfo in sessionInfos)
                {
                    WtsApi32.WTSDisconnectSession(server, sessionInfo.SessionID, true);
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
                WindowsSessionInfo sessionInfo = GetWindowsSessions(server)
                    .Find(
                        p => p.Username != null &&
                        string.Compare(p.Username.Trim().ToLowerInvariant(), username.Trim().ToLowerInvariant()) == 0
                    );

                if (sessionInfo != null)
                    return WtsApi32.WTSDisconnectSession(server, sessionInfo.SessionID, true);

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
                List<WindowsSessionInfo> sessionInfos = GetWindowsSessions(server);
                foreach (var sessionInfo in sessionInfos)
                {
                    WtsApi32.WTSLogoffSession(server, sessionInfo.SessionID, true);
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
                WindowsSessionInfo sessionInfo = GetWindowsSessions(server)
                    .Find(
                        p => p.Username != null &&
                        string.Compare(p.Username.Trim().ToLowerInvariant(), username.Trim().ToLowerInvariant()) == 0
                    );

                if (sessionInfo != null)
                    return WtsApi32.WTSLogoffSession(server, sessionInfo.SessionID, true);

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

        public static bool Run(RunInfo runInfo)
        {
            if (string.IsNullOrWhiteSpace(runInfo.Application))
            {
                LoggerHelper.Debug("WindowsAPI::Run() - Empty Application parameter. Returning.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(runInfo.CommandLine))
                runInfo.CommandLine = null;

            if (string.IsNullOrWhiteSpace(runInfo.WorkingDir))
                runInfo.WorkingDir = null;

            IntPtr server = GetServerPtr();
            IntPtr hUserToken = IntPtr.Zero;
            IntPtr pEnv = IntPtr.Zero;

            try
            {
                WindowsSessionInfo sessionInfo = GetUserActiveSession(server, runInfo.Username);
                if (sessionInfo == null && (runInfo.Fallback || string.IsNullOrWhiteSpace(runInfo.Username)))
                {
                    LoggerHelper.Debug("WindowsAPI::Run() - User session not found, trying to get first active session.");
                    sessionInfo = GetFirstActiveSession(server);
                }

                if (sessionInfo == null)
                {
                    LoggerHelper.Warn("WindowsAPI::Run() - No User/Active session found. Returning.");
                    return false;
                }

                if (!GetSessionUserToken(server, sessionInfo, ref hUserToken))
                {
                    LoggerHelper.Error("WindowsAPI::Run() - Cannot get session user token.");
                    return false;
                }

                if (!UserEnv.CreateEnvironmentBlock(ref pEnv, hUserToken, false))
                {
                    LoggerHelper.Error("WindowsAPI::Run() - Cannot create environment block.");
                    return false;
                }

                // Launch the child process interactively using the token of the logged user. 
                ProcessInformation tProcessInfo;

                // Startup flags
                StartupInfo tStartUpInfo = new StartupInfo();
                tStartUpInfo.wShowWindow = (short)(runInfo.Visible ? SW.SW_SHOW : SW.SW_HIDE);
                tStartUpInfo.cb = StartupInfo.SizeOf;
                tStartUpInfo.lpDesktop = "winsta0\\default";

                // Creation Flags
                uint dwCreationFlags = CREATE_UNICODE_ENVIRONMENT | (uint)(runInfo.Visible ? CREATE_NEW_CONSOLE : CREATE_NO_WINDOW);

                bool childProcStarted = AdvApi32.CreateProcessAsUser(
                            hUserToken,                    // Token of the logged-on user. 
                            runInfo.Application,           // Name of the process to be started. 
                            runInfo.CommandLine,           // Any command line arguments to be passed. 
                            IntPtr.Zero,                   // Default Process' attributes. 
                            IntPtr.Zero,                   // Default Thread's attributes. 
                            false,                         // Does NOT inherit parent's handles. 
                            dwCreationFlags,               // No any specific creation flag. 
                            pEnv,                          // Default environment path. 
                            runInfo.WorkingDir,            // Default current directory. 
                            ref tStartUpInfo,              // Process Startup Info.  
                            out tProcessInfo               // Process information to be returned. 
                            );

                if (childProcStarted)
                {
                    LoggerHelper.Debug("WindowsAPI::Run() - Process seems to be started.");
                    Kernel32.CloseHandle(tProcessInfo.hThread);
                    Kernel32.CloseHandle(tProcessInfo.hProcess);
                }
                else
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    LoggerHelper.Error("WindowsAPI::Run() - CreateProcessAsUser failed. Error Code: {0}", errorCode);
                }

                return childProcStarted;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("WindowsAPI::Run() - Exception: {0}", ex.ToString());
                return false;
            }
            finally
            {
                if (pEnv != IntPtr.Zero)
                    UserEnv.DestroyEnvironmentBlock(pEnv);

                if (hUserToken != IntPtr.Zero)
                    Kernel32.CloseHandle(hUserToken);

                WtsApi32.WTSCloseServer(server);
            }
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
                    di.Availability = mi.Flags.ToString();
                    displays.Add(di);
                }
                return true;
            }, IntPtr.Zero);

            return displays;
        }

        public static List<WindowsSessionInfo> GetWindowsSessions()
        {
            IntPtr server = GetServerPtr();
            try
            {
                return GetWindowsSessions(server);
            }
            finally
            {
                WtsApi32.WTSCloseServer(server);
            }
        }

        private static bool GetSessionUserToken(IntPtr server, WindowsSessionInfo sessionInfo, ref IntPtr phUserToken)
        {
            var hImpersonationToken = IntPtr.Zero;
            bool bResult = false;
            if (WtsApi32.WTSQueryUserToken(sessionInfo.SessionID, out hImpersonationToken))
            {
                bResult = AdvApi32.DuplicateTokenEx(hImpersonationToken, 0, IntPtr.Zero, (int)SecurityImpersonationLevel.SecurityImpersonation, (int)TokenType.TokenPrimary, ref phUserToken);
                Kernel32.CloseHandle(hImpersonationToken);
            }

            return bResult;
        }

        private static List<WindowsSessionInfo> GetWindowsSessions(IntPtr server)
        {
            int sessionCount = 0;
            IntPtr pSessionInfo = IntPtr.Zero;
            List<WindowsSessionInfo> sessionInfos = new List<WindowsSessionInfo>();

            try
            {
                if (WtsApi32.WTSEnumerateSessions(server, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
                    IntPtr current = pSessionInfo;
                    for (int i = 0; i < sessionCount; i++)
                    {
                        WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
                        current += dataSize;
                        WindowsSessionInfo sessionInfo = new WindowsSessionInfo();
                        sessionInfo.SessionID = si.SessionID;
                        sessionInfo.StationName = si.pWinStationName;
                        sessionInfo.IsActive = si.State == WtsApi32.WtsConnectStateClass.WTSActive;
                        sessionInfo.Username = GetUsername(sessionInfo.SessionID);

                        sessionInfos.Add(sessionInfo);
                    }
                }
            }
            finally
            {
                WtsApi32.WTSFreeMemory(pSessionInfo);
            }

            return sessionInfos;
        }

        private static WindowsSessionInfo GetFirstActiveSession(IntPtr server)
        {
            int sessionCount = 0;
            IntPtr pSessionInfo = IntPtr.Zero;

            try
            {
                if (WtsApi32.WTSEnumerateSessions(server, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
                    IntPtr current = pSessionInfo;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
                        current += dataSize;
                        if (si.State != WtsApi32.WtsConnectStateClass.WTSActive)
                            continue;

                        WindowsSessionInfo sessionInfo = new WindowsSessionInfo();
                        sessionInfo.SessionID = si.SessionID;
                        sessionInfo.StationName = si.pWinStationName;
                        sessionInfo.IsActive = si.State == WtsApi32.WtsConnectStateClass.WTSActive;
                        sessionInfo.Username = GetUsername(sessionInfo.SessionID);

                        return sessionInfo;
                    }
                }
            }
            finally
            {
                WtsApi32.WTSFreeMemory(pSessionInfo);
            }

            return null;
        }

        private static WindowsSessionInfo GetUserActiveSession(IntPtr server, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            int sessionCount = 0;
            IntPtr pSessionInfo = IntPtr.Zero;

            try
            {
                if (WtsApi32.WTSEnumerateSessions(server, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
                    IntPtr current = pSessionInfo;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
                        current += dataSize;
                        if (si.State != WtsApi32.WtsConnectStateClass.WTSActive)
                            continue;

                        string sessionUser = GetUsername(si.SessionID);
                        if (string.IsNullOrWhiteSpace(sessionUser) || string.Compare(username.Trim().ToLowerInvariant(), sessionUser.Trim().ToLowerInvariant()) != 0)
                            continue;

                        WindowsSessionInfo sessionInfo = new WindowsSessionInfo();
                        sessionInfo.SessionID = si.SessionID;
                        sessionInfo.StationName = si.pWinStationName;
                        sessionInfo.IsActive = si.State == WtsApi32.WtsConnectStateClass.WTSActive;
                        sessionInfo.Username = sessionUser;

                        return sessionInfo;
                    }
                }
            }
            finally
            {
                WtsApi32.WTSFreeMemory(pSessionInfo);
            }

            return null;
        }

        private static IntPtr GetServerPtr()
        {
            return WtsApi32.WTSOpenServer(Environment.MachineName);
        }
    }
}
