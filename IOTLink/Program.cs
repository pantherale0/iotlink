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
                ServiceController service = ServiceController.GetServices().First(s => s.ServiceName == "IOTLink");

                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        InstallService(service);
                        break;
                    case "--uninstall":
                        UninstallService(service);
                        break;
                    case "--restart":
                        RestartService(service);
                        break;
                    case "--stop":
                        RestartService(service);
                        break;
                    default:
                        CheckService(service);
                        break;
                }
            }
            else
            {
                ServiceBase service = new IOTLinkService();
                ServiceBase.Run(service);
            }
        }

        private static void InstallService(ServiceController controller)
        {
            try
            {
                if (controller != null)
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

        private static void RestartService(ServiceController controller)
        {
            try
            {
                if (controller == null)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Cannot (re)start service. Service not installed.");
                    return;
                }

                if (controller.Status != ServiceControllerStatus.Stopped)
                    ChangeServiceStatus(controller, ServiceControllerStatus.Stopped);

                ChangeServiceStatus(controller, ServiceControllerStatus.Running);
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Start Service failed. Please, run as an administrator.");
            }
        }

        private static void StopService(ServiceController controller)
        {
            try
            {
                if (controller == null)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Cannot stop service. Service not installed.");
                    return;
                }

                if (controller.Status != ServiceControllerStatus.Stopped)
                    ChangeServiceStatus(controller, ServiceControllerStatus.Stopped);
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Start Service failed. Please, run as an administrator.");
            }
        }

        private static void CheckService(ServiceController controller)
        {
            try
            {
                if (controller == null)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Service not installed.");
                    return;
                }

                ServiceControllerStatus currentStatus = controller.Status;
                WindowsAPI.ShowMessage("Service Installer", "Current Status: " + currentStatus.ToString());
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Service check. Please, run as an administrator.");
            }
        }

        private static void UninstallService(ServiceController controller)
        {
            try
            {
                if (controller == null)
                {
                    WindowsAPI.ShowMessage("Service Installer", "Service not found.");
                    return;
                }
                ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                WindowsAPI.ShowMessage("Service Installer", "Service is uninstalled sucessfully.");
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Uninstall failed. Please, run as an administrator.");
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggerHelper.Critical("Critical Unhandled Exception: " + e.ExceptionObject.ToString());
            LoggerHelper.GetInstance().Flush();
        }

        private static void ChangeServiceStatus(ServiceController serviceController, ServiceControllerStatus desiredStatus)
        {
            try
            {
                ServiceControllerStatus currentStatus = serviceController.Status;
                switch (currentStatus)
                {
                    case ServiceControllerStatus.ContinuePending:
                    case ServiceControllerStatus.StartPending:
                    case ServiceControllerStatus.Running:
                        if (desiredStatus == ServiceControllerStatus.Stopped)
                            serviceController.Stop();
                        if (desiredStatus == ServiceControllerStatus.Paused)
                            serviceController.Pause();
                        break;

                    case ServiceControllerStatus.StopPending:
                    case ServiceControllerStatus.Stopped:
                        if (desiredStatus == ServiceControllerStatus.Running)
                            serviceController.Start();
                        break;

                    case ServiceControllerStatus.PausePending:
                    case ServiceControllerStatus.Paused:
                        if (desiredStatus == ServiceControllerStatus.Stopped)
                            serviceController.Stop();
                        if (desiredStatus == ServiceControllerStatus.Running)
                            serviceController.Start();
                        break;

                    default: break;
                }

                if (serviceController.Status != desiredStatus)
                    serviceController.WaitForStatus(desiredStatus, new TimeSpan(0, 0, 0, 60));
            }
            catch (Exception)
            {
                WindowsAPI.ShowMessage("Service Installer", "Failed to change status. Please, run as an administrator.");
            }
        }
    }
}
