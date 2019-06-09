using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IOTLink.Platform.Windows.Native
{
    public class User32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);
    }
}
