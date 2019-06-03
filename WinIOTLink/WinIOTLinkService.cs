using System;
using System.ServiceProcess;
using WinIOTLink.Engine;
using WinIOTLink.Helpers;

namespace WinIOTLink
{
    public partial class WinIOTLinkService : ServiceBase
    {
        public WinIOTLinkService()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            LoggerHelper.Info("WinIOTLink", "Service is started.");
            MainEngine.GetInstance().StartApplication(ConfigHelper.GetApplicationConfig());
        }

        protected override void OnStop()
        {
            LoggerHelper.Info("WinIOTLink", "Service is stopped.");
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            String username = WindowsHelper.GetUsername(changeDescription.SessionId);
            MainEngine.GetInstance().OnSessionChange(username, changeDescription.Reason);
        }
    }
}
