using IOTLinkAddon.Common;
using IOTLinkAddon.Common.Helpers;
using IOTLinkAddon.Common.Processes;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace IOTLinkAddon.Agent
{
    public class ProcessMonitorAgent : AgentAddon
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);
            OnAgentRequestHandler += OnAgentRequest;
        }

        private void OnAgentRequest(object sender, AgentAddonRequestEventArgs e)
        {
            LoggerHelper.Verbose("WindowsMonitorAgent::OnAgentRequest");

            AddonRequestType requestType = e.Data.requestType;
            switch (requestType)
            {
                case AddonRequestType.REQUEST_PROCESS_INFORMATION:
                    ExecuteProcessInformation(e.Data.processId);
                    break;

                default: break;
            }
        }

        private void ExecuteProcessInformation(dynamic data)
        {
            try
            {
                int processId = (int)data;
                ProcessInformation process = ProcessHelper.GetProcessInformation(processId, false);
                if (process == null)
                    return;

                LoggerHelper.Debug("ProcessMonitorAgent::ExecuteProcessInformation({0}) - Handle: {1} | Title: {2}", processId, process.MainWindowHandle, process.MainWindowTitle);

                IntPtr mainHwnd = new IntPtr(process.MainWindowHandle);
                List<IntPtr> handles = new List<IntPtr>();
                handles.Add(mainHwnd);
                handles.AddRange(WindowsAPI.GetChildWindows(mainHwnd));

                process.FullScreen = handles.Any(x => WindowsAPI.IsFullScreen(x));
                if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    process.MainWindowTitle = process.MainWindowTitle.Trim(new char[] { '\r', '\n', '\t' }).Trim();

                dynamic addonData = new ExpandoObject();
                addonData.requestType = AddonRequestType.REQUEST_PROCESS_INFORMATION;
                addonData.requestData = process;

                GetManager().SendAgentResponse(this, addonData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ProcessMonitorAgent::ExecuteProcessInformation() - Error: {0}", ex);
            }
        }
    }
}
