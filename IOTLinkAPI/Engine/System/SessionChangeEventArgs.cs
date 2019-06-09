using System;
using System.ServiceProcess;

namespace IOTLink.Engine.System
{
    public class SessionChangeEventArgs : EventArgs
    {
        public string Username { get; set; }
        public SessionChangeReason Reason { get; set; }
    }
}
