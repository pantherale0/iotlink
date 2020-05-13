using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using static System.Management.ManagementObjectCollection;

namespace IOTLinkAddon.Service.Platform
{
    public static class ProcessHelper
    {
        public static readonly string DEFAULT_SCOPE = @"\\.\root\CIMV2";
        public static readonly ManagementScope DEFAULT_SCOPE_OBJECT = new ManagementScope(DEFAULT_SCOPE);

        public static List<ProcessInformation> GetProcesses()
        {
            Process[] processes = Process.GetProcesses();

            return processes.Select(x => ParseProcess(x)).ToList();
        }

        public static List<ProcessInformation> GetProcessesByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return new List<ProcessInformation>();

            processName = CleanProcessName(processName);
            Process[] processes = Process.GetProcessesByName(processName);

            return processes.Select(x => ParseProcess(x)).ToList();
        }

        public static ProcessInformation GetProcessInformation(int processId)
        {
            try
            {
                if (processId == 0)
                    return null;

                Process process = Process.GetProcessById(processId);
                if (process == null || process.Id == 0)
                    return null;

                return ParseProcess(process);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ProcessInformation GetProcessParent(int processId)
        {
            try
            {
                var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", processId);
                ManagementObjectSearcher search = new ManagementObjectSearcher(DEFAULT_SCOPE, query);
                ManagementObjectCollection resultCollection = search.Get();

                if (resultCollection.Count == 0)
                    return null;

                ManagementObjectEnumerator results = resultCollection.GetEnumerator();
                if (!results.MoveNext())
                    return null;

                int parentProcessId = Convert.ToInt32((uint)results.Current["ParentProcessId"]);
                return GetProcessInformation(parentProcessId);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ProcessHelper::GetProcessParent({0}) - Error: {1}", processId, ex);
                return null;
            }
        }

        public static string CleanProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            return processName.ToLowerInvariant().Replace(".exe", "").Trim();
        }

        private static ProcessInformation ParseProcess(Process process)
        {
            ProcessInformation result = new ProcessInformation
            {
                Id = process.Id,
                SessionId = process.SessionId,
                ProcessName = process.ProcessName,
                Status = ParseState(process),
                Parent = GetProcessParent(process.Id)
            };

            if (!process.HasExited)
            {
                result.StartDateTime = process.StartTime;
                result.MemoryUsed = process.WorkingSet64;
                result.ProcessorUsage = process.TotalProcessorTime;
                result.MainWindowTitle = process.MainWindowTitle;
                result.FullScreen = false; //TODO: Implement fullscreen detection
            }

            return result;
        }

        private static ProcessState ParseState(Process process)
        {
            return process.HasExited ? ProcessState.NotRunning : ProcessState.Running;
        }
    }
}
