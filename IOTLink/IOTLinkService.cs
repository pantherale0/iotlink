using System;
using System.ServiceProcess;
using IOTLink.Engine;
using IOTLink.Helpers;

namespace IOTLink
{
    public partial class IOTLinkService : ServiceBase
    {
        public IOTLinkService()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            LoggerHelper.Info(typeof(IOTLinkService), "Windows Service is started.");
            MainEngine.GetInstance().StartApplication();
        }

        protected override void OnStop()
        {
            LoggerHelper.Info(typeof(IOTLinkService), "Windows Service is stopped.");
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
