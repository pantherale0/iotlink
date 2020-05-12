using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IOTLinkAddon.Service.Platform
{
    public static class ProcessHelper
    {
        public static List<ProcessInformation> GetProcesses()
        {
            Process[] processes = Process.GetProcesses();

            return processes.Select(x => ParseProcess(x)).ToList();
        }

        public static List<ProcessInformation> GetProcessesByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            return processes.Select(x => ParseProcess(x)).ToList();
        }

        public static ProcessInformation GetProcessInformation(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                if (process == null)
                    return null;

                return ParseProcess(process);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ProcessHelper::GetProcessInformation({0}) - Error: {1}", processId, ex);
                return null;
            }
        }

        private static ProcessInformation ParseProcess(Process process)
        {
            return new ProcessInformation
            {
                Id = process.Id,
                SessionId = process.SessionId,
                ProcessName = process.ProcessName,
                StartDateTime = process.StartTime,
                MemoryUsed = process.WorkingSet64,
                ProcessorUsage = process.TotalProcessorTime,
                MainWindowTitle = process.MainWindowTitle,
                Status = ParseState(process)
            };
        }

        private static ProcessState ParseState(Process process)
        {
            if (process.HasExited)
                return ProcessState.Exited;

            return (process.Responding ? ProcessState.Running : ProcessState.Suspended);
        }
    }
}
