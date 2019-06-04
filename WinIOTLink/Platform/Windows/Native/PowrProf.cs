using System.Runtime.InteropServices;

namespace WinIOTLink.Platform.Windows.Native
{
    public class PowrProf
    {
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
    }
}
