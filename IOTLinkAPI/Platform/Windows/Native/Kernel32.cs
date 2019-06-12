﻿using IOTLink.Platform.Windows.Native.Internal;
using System;
using System.Runtime.InteropServices;

namespace IOTLink.Platform.Windows.Native
{
#pragma warning disable 1591
    public class Kernel32
    {
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);
    }
}
