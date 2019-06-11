using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IOTLink.Platform.Windows.Native
{
    public class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LastInputInfo
        {
            public static readonly uint SizeOf = (uint)Marshal.SizeOf(typeof(LastInputInfo));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LastInputInfo plii);
    }
}
