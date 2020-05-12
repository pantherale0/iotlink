using System;

namespace IOTLinkAddon.Common
{
    public class ProcessEventArgs : EventArgs
    {
        public string ProcessName { get; set; }
        public int ProcessID { get; set; }
        public int ParentProcessID { get; set; }

        public ProcessEventArgs(string processName, int processId, int parentProcessId) : base()
        {
            ProcessName = processName;
            ProcessID = processId;
            ParentProcessID = parentProcessId;
        }
    }
}
