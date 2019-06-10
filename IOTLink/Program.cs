using IOTLink.Helpers;
using IOTLink.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

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

            // Service Run
            if (!Environment.UserInteractive)
            {
                ServiceBase service = new IOTLinkService();
                ServiceBase.Run(service);
                return;
            }

            if (args.Length == 0)
            {
                WindowsAPI.ShowMessage("IOT Link", "Missing command-line parameters.");
                return;
            }

            Queue<string> argsQueue = new Queue<string>(args);
            while (argsQueue.Count > 0)
            {
                // Ignore all command line arguments until we find an argument
                // which starts with '--' characters
                while (argsQueue.Count > 0 && !argsQueue.Peek().StartsWith("--"))
                    argsQueue.Dequeue();

                string command = argsQueue.Dequeue().ToLowerInvariant();

                // Parse command line to get all commands arguments
                List<string> arguments = new List<string>();
                while (argsQueue.Count > 0 && !argsQueue.Peek().StartsWith("--"))
                    arguments.Add(argsQueue.Dequeue());

                OnCommand(command, arguments);
            }
        }

        private static void OnCommand(string command, List<string> arguments)
        {
            switch (command)
            {
                case "--install":
                    OnServiceInstallCommand();
                    break;

                case "--uninstall":
                    OnServiceUninstallCommand();
                    break;

                case "--check":
                    OnServiceCheckCommand();
                    break;

                case "--test":
                    OnTestCommand(arguments);
                    break;

                default: break;
            }
        }

        private static void OnTestCommand(List<string> arguments)
        {
            WindowsAPI.ShowMessage("IOT Link", string.Join(" ", arguments));
        }

        private static void OnServiceInstallCommand()
        {
            if (!Environment.UserInteractive)
                return;

            try
            {
                bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == "IOTLink");
                if (serviceExists)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Service is already installed.");
                    return;
                }

                ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                WindowsAPI.ShowMessage("Service Installer", "Service is installed sucessfully.");
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Install failed. Please, run as an administrator.");
            }
        }

        private static void OnServiceUninstallCommand()
        {
            if (!Environment.UserInteractive)
                return;

            try
            {
                bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == "IOTLink");
                if (!serviceExists)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Service is already not installed.");
                    return;
                }

                ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                WindowsAPI.ShowMessage("Service Installer", "Service is uninstalled sucessfully.");
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Install failed. Please, run as an administrator.");
            }
        }

        private static void OnServiceCheckCommand()
        {
            if (!Environment.UserInteractive)
                return;

            try
            {
                bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == "IOTLink");
                if (serviceExists)
                    WindowsAPI.ShowMessage("Service Installer", "Service is installed.");
                else
                    WindowsAPI.ShowMessage("Service Installer", "Service is not installed.");
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Please, run as an administrator.");
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggerHelper.Critical("Critical Unhandled Exception: " + e.ExceptionObject.ToString());
            LoggerHelper.GetInstance().Flush();
        }
    }
}