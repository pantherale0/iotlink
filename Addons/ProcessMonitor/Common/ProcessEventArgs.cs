using System;

namespace IOTLinkAddon.Common
{
    public class ProcessEventArgs : EventArgs
    {
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public int ParentProcessId { get; set; }
        public int SessionId { get; set; }

        public ProcessEventArgs(int sessionId, string processName, int processId, int parentProcessId) : base()
        {
            ProcessName = processName;
            ProcessId = processId;
            ParentProcessId = parentProcessId;
        }
    }
}
