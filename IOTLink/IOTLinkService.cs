using System;
using System.ServiceProcess;
using IOTLinkAPI.Helpers;
using IOTLinkService.Engine;

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
            LoggerHelper.Info("Windows Service is started.");
            MainEngine.GetInstance().StartApplication();
        }

        protected override void OnStop()
        {
            LoggerHelper.Info("Windows Service is stopped.");
            LoggerHelper.EmptyLine();

            MainEngine.GetInstance().StopApplication();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            string username = PlatformHelper.GetUsername(changeDescription.SessionId);
            MainEngine.GetInstance().OnSessionChange(username, changeDescription.SessionId, changeDescription.Reason);
        }
    }
}
