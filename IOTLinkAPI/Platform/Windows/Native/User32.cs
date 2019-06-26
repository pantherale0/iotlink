using IOTLinkAPI.Platform.Windows.Native.Internal;
using System;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native
{
    public class User32
    {
        public const int CCHDEVICENAME = 32;
        public delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, [Out] IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetLastInputInfo(ref LastInputInfo plii);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);
    }
}
