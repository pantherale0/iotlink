using IOTLinkAPI.Platform.Windows;
using System;
using System.Linq;
using System.ServiceProcess;

namespace IOTLinkService.Service.Commands
{
    class ServiceCheck : ICommand
    {
        private const string COMMAND_LINE = "check";

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
                if (serviceExists)
                    WindowsAPI.ShowMessage("Service Installer", "Service is installed.");
                else
                    WindowsAPI.ShowMessage("Service Installer", "Service is not installed.");

                return 0;
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Please, run as an administrator.");
            }

            return -1;
        }
    }
}
