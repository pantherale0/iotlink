﻿using IOTLinkAddon.Common;
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
                ProcessInformation processInfo = ProcessHelper.GetProcessInformation(processId, false);
                if (processInfo == null)
                    return;

                LoggerHelper.Debug("ProcessMonitorAgent::ExecuteProcessInformation({0}) - Handle: {1} | Title: {2}", processId, processInfo.MainWindowHandle, processInfo.MainWindowTitle);

                dynamic addonData = new ExpandoObject();
                addonData.requestType = AddonRequestType.REQUEST_PROCESS_INFORMATION;
                addonData.requestData = FillProcessInformation(processInfo);

                GetManager().SendAgentResponse(this, addonData);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ProcessMonitorAgent::ExecuteProcessInformation() - Error: {0}", ex);
            }
        }

        private ProcessInformation FillProcessInformation(ProcessInformation processInfo)
        {
            if (processInfo == null)
                return null;

            List<ProcessInformation> childrenProcessInfos = ProcessHelper.GetProcessChildren(processInfo.Id);
            List<IntPtr> handles = GetProcessHandles(processInfo);

            foreach (ProcessInformation cpi in childrenProcessInfos)
            {
                var childrenHandles = GetProcessHandles(cpi);
                handles.AddRange(childrenHandles);
            }

            processInfo.MainWindowTitle = GetProcessTitle(processInfo);
            processInfo.FullScreen = IsFullScreen(handles);
            processInfo.Windows = GetProcessWindows(handles);
            processInfo.ClassNames = GetProcessClassNames(handles);

            return processInfo;
        }

        private List<IntPtr> GetProcessHandles(ProcessInformation process)
        {
            if (process == null)
                return new List<IntPtr>();

            var mainHwnd = new IntPtr(process.MainWindowHandle);
            var handles = new List<IntPtr>();
            handles.Add(mainHwnd);
            handles.AddRange(WindowsAPI.GetChildWindows(mainHwnd));

            return handles;
        }

        private string GetProcessTitle(ProcessInformation process)
        {
            if (process == null)
                return null;

            if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                return process.MainWindowTitle.Trim(new char[] { '\r', '\n', '\t' }).Trim();

            return null;
        }

        private bool IsFullScreen(List<IntPtr> handles)
        {
            return handles.Any(x => WindowsAPI.IsFullScreen(x));
        }

        private List<string> GetProcessWindows(List<IntPtr> handles)
        {
            return handles.Select(h => WindowsAPI.GetWindowTitle(h)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        private List<string> GetProcessClassNames(List<IntPtr> handles)
        {
            return handles.Select(h => WindowsAPI.GetWindowClassName(h)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
    }
}
