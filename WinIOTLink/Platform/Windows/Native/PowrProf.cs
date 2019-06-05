using System.Runtime.InteropServices;

namespace WinIOTLink.Platform.Windows.Native
{
    public class PowrProf
    {
        [DllImport("powrprof.dll")]
        static extern bool IsPwrHibernateAllowed();

        [DllImport("powrprof.dll")]
        static extern bool IsPwrShutdownAllowed();

        [DllImport("powrprof.dll")]
        static extern bool IsPwrSuspendAllowed();

        [DllImport("powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
    }
}
