using IOTLinkAddon.Common;
using IOTLinkAPI.Helpers;
using System.Management;
using static IOTLinkAddon.Common.ProcessHandlers;

namespace IOTLinkAddon.Service.Monitors
{
    class ProcessMonitor
    {
        ManagementEventWatcher _startWatcher;
        ManagementEventWatcher _stopWatcher;

        public event ProcessStartedEventHandler OnProcessStarted;
        public event ProcessStoppedEventHandler OnProcessStopped;

        public void Init()
        {
            LoggerHelper.Info("ProcessMonitor::Init() - Initializing Process Monitoring");

            if (_startWatcher == null)
            {
                LoggerHelper.Debug("ProcessMonitor::Init() - Creating OnProcessStarted Event");

                _startWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                _startWatcher.EventArrived += OnProcessStartedWatcherEvent;
            }

            if (_stopWatcher == null)
            {
                LoggerHelper.Debug("ProcessMonitor::Init() - Creating OnProcessStopped Event");

                _stopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                _stopWatcher.EventArrived += OnProcessStoppedWatcherEvent;
            }

            OnProcessStarted = null;
            OnProcessStopped = null;

            _startWatcher.Start();
            _stopWatcher.Start();
        }

        private void OnProcessStartedWatcherEvent(object sender, EventArrivedEventArgs e)
        {
            long processId = (long)e.NewEvent.Properties["ProcessID"].Value;
            long parentProcessId = (long)e.NewEvent.Properties["ParentProcessID"].Value;
            LoggerHelper.Debug("ProcessMonitor::OnProcessStarted() - ProcessID: {0} ParentProcessID: {1}", processId, parentProcessId);

            ProcessEventArgs eventArgs = new ProcessEventArgs(processId, parentProcessId);
            OnProcessStarted?.Invoke(this, eventArgs);
        }

        private void OnProcessStoppedWatcherEvent(object sender, EventArrivedEventArgs e)
        {
            long processId = (long)e.NewEvent.Properties["ProcessID"].Value;
            long parentProcessId = (long)e.NewEvent.Properties["ParentProcessID"].Value;
            LoggerHelper.Debug("ProcessMonitor::OnProcessStopped() - ProcessID: {0} ParentProcessID: {1}", processId, parentProcessId);

            ProcessEventArgs eventArgs = new ProcessEventArgs(processId, parentProcessId);
            OnProcessStopped?.Invoke(this, eventArgs);
        }
    }
}
