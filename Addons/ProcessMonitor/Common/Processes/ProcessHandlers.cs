namespace IOTLinkAddon.Common.Processes
{
    public abstract class ProcessHandlers
    {
        public delegate void ProcessStartedEventHandler(object sender, ProcessEventArgs e);
        public delegate void ProcessStoppedEventHandler(object sender, ProcessEventArgs e);
    }
}
