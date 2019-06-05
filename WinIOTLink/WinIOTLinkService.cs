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
            LoggerHelper.Info(typeof(WinIOTLinkService), "Windows Service is started.");
            MainEngine.GetInstance().StartApplication();
        }

        protected override void OnStop()
        {
            LoggerHelper.Info(typeof(WinIOTLinkService), "Windows Service is stopped.");
            LoggerHelper.EmptyLine();

            MainEngine.GetInstance().StopApplication();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            String username = WindowsHelper.GetUsername(changeDescription.SessionId);
            MainEngine.GetInstance().OnSessionChange(username, changeDescription.Reason);
        }
    }
}
