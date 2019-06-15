using IOTLinkAPI.Platform.Windows;
using System;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace IOTLinkService.Service.Commands
{
    class ServiceUninstall : ICommand
    {
        private const string COMMAND_LINE = "uninstall";

        public string GetCommandLine()
        {
            return COMMAND_LINE;
        }

        public int ExecuteCommand(string[] args)
        {
            if (!Environment.UserInteractive)
                return -1;

            try
            {
                bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == "IOTLink");
                if (!serviceExists)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Service is currently not installed.");
                    return -1;
                }

                ManagedInstallerClass.InstallHelper(new string[] { "/u", "/LogFile=", "/LogToConsole=false", Assembly.GetExecutingAssembly().Location });
                WindowsAPI.ShowMessage("Service Installer", "Service is uninstalled sucessfully.");
                return 0;
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Uninstall failed. Please, run as an administrator.");
            }

            return -1;
        }
    }
}
