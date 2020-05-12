using System;

namespace IOTLinkAddon.Common
{
    public class ProcessEventArgs : EventArgs
    {
        public long ProcessID { get; set; }

        public long ParentProcessID { get; set; }

        public ProcessEventArgs(long processId, long parentProcessId) : base()
        {
            ProcessID = processId;
            ParentProcessID = parentProcessId;
        }
    }
}
