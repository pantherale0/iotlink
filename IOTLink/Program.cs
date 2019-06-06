using System;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using IOTLink.Helpers;
using IOTLink.Platform.Windows;

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
                bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == "IOTLink");

                string parameter = string.Concat(args);
                if (serviceExists)
                {
                    switch (parameter)
                    {
                        case "--install":
                            WindowsAPI.ShowMessage("Service Installer", "IOTLink Service is already installed.");
                            break;
                        case "--uninstall":
                            try
                            {
                                ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                WindowsAPI.ShowMessage("Service Installer", "IOTLink Service is uninstalled sucessfully.");
                            }
                            catch (Exception)
                            {
                                WindowsAPI.ShowMessage("Service Installer", "Uninstall failed. Please, run as an administrator.");
                            }
                            break;
                        default:
                            WindowsAPI.ShowMessage("Service Installer", serviceExists ? "IOTLink Service is installed." : "IOTLink Service is NOT installed.");
                            break;
                    }
                }
                else
                {
                    switch (parameter)
                    {
                        case "--install":
                            try
                            {
                                ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                WindowsAPI.ShowMessage("Service Installer", "IOTLink Service is installed sucessfully.");
                            }
                            catch (Exception)
                            {
                                WindowsAPI.ShowMessage("Service Installer", "Install failed. Please, run as an administrator.");
                            }
                            break;
                        case "--uninstall":
                            WindowsAPI.ShowMessage("Service Installer", "IOTLink Service not found.");
                            break;
                        default:
                            WindowsAPI.ShowMessage("Service Installer", serviceExists ? "IOTLink Service is installed." : "IOTLink Service is NOT installed.");
                            break;
                    }
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
            LoggerHelper.Critical("Critical Unhandled Exception: " + e.ExceptionObject.ToString());
            LoggerHelper.GetInstance().Flush();
        }
    }
}
