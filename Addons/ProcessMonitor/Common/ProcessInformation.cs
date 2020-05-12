using System;

namespace IOTLinkAddon.Service.Platform
{
    public class ProcessInformation
    {
        public long Id { get; set; }
        public int SessionId { get; set; }
        public string ProcessName { get; set; }
        public DateTime StartDateTime { get; set; }
        public long MemoryUsed { get; set; }
        public TimeSpan ProcessorUsage { get; set; }
        public string MainWindowTitle { get; set; }
        public bool FullScreen { get; set; }
        public ProcessState Status { get; set; }
    }
}
