using IOTLinkAPI.Helpers;
using IOTLinkService.Service.Engine;
using System.ServiceProcess;

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
            ServiceMain.GetInstance().StartApplication();
        }

        protected override void OnStop()
        {
            LoggerHelper.Info("Windows Service is stopped.");
            LoggerHelper.EmptyLine();

            ServiceMain.GetInstance().StopApplication();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            string username = PlatformHelper.GetUsername(changeDescription.SessionId);
            ServiceMain.GetInstance().OnSessionChange(username, changeDescription.SessionId, changeDescription.Reason);
        }
    }
}
