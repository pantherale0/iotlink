using System;
using System.Collections.Generic;

namespace IOTLinkAddon.Common.Processes
{
    public class ProcessInformation
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string ProcessName { get; set; }
        public DateTime StartDateTime { get; set; }
        public long MemoryUsed { get; set; }
        public TimeSpan ProcessorUsage { get; set; }
        public int MainWindowHandle { get; set; }
        public string MainWindowTitle { get; set; }
        public bool FullScreen { get; set; }
        public ProcessState Status { get; set; }
        public ProcessInformation Parent { get; set; }
        public List<string> Windows { get; set; }
        public List<string> ClassNames { get; set; }
    }
}
