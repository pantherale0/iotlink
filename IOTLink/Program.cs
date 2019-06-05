using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using IOTLink.Helpers;

namespace IOTLink
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    default: break;
                }
            }
            else
            {
                ServiceBase service = new IOTLinkService();
                ServiceBase.Run(service);
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggerHelper.Critical(typeof(Program), "Critical Unhandled Exception: " + e.ExceptionObject.ToString());
            LoggerHelper.GetInstance().Flush();
        }
    }
}
