using System;
using System.ServiceProcess;

namespace WinIOTLink.Engine.System
{
    public class SessionChangeEventArgs : EventArgs
    {
        public string Username { get; set; }
        public SessionChangeReason Reason { get; set; }
    }
}
