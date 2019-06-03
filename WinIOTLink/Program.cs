using System.ServiceProcess;

namespace WinIOTLink
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new WinIOTLinkService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
