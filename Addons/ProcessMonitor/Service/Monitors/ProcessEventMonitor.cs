using IOTLinkAddon.Common.Helpers;
using IOTLinkAddon.Common.Processes;
using IOTLinkAPI.Helpers;
using System;
using System.Management;
using static IOTLinkAddon.Common.Processes.ProcessHandlers;

namespace IOTLinkAddon.Service.Monitors
{
    class ProcessEventMonitor
    {
        ManagementEventWatcher _eventWatcher;

        public event ProcessStartedEventHandler OnProcessStarted;
        public event ProcessStoppedEventHandler OnProcessStopped;

        public void Init()
        {
            LoggerHelper.Info("ProcessEventMonitor::Init() - Initializing Process Monitoring");

            if (_eventWatcher == null)
            {
                LoggerHelper.Info("ProcessEventMonitor::Init() - Registering Process Events Watcher.");

                var query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'");
                _eventWatcher = new ManagementEventWatcher(ProcessHelper.DEFAULT_SCOPE_OBJECT, query);
                _eventWatcher.EventArrived += new EventArrivedEventHandler(OnProcessWatcherEvent);
            }

            _eventWatcher.Start();
        }

        private void OnProcessWatcherEvent(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.ClassPath.ClassName.Contains("InstanceCreationEvent"))
                CreateProcessStartEvent((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value);
            else if (e.NewEvent.ClassPath.ClassName.Contains("InstanceDeletionEvent"))
                CreateProcessStopEvent((ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value);
        }

        private void CreateProcessStartEvent(ManagementBaseObject baseObject)
        {
            int processId = Convert.ToInt32(baseObject.Properties["ProcessId"].Value);
            int parentProcessId = Convert.ToInt32(baseObject.Properties["ParentProcessId"].Value);
            int sessionId = Convert.ToInt32(baseObject.Properties["SessionId"].Value);
            string processName = (string)baseObject.Properties["Name"].Value;
            LoggerHelper.Debug("ProcessEventMonitor::CreateProcessStartEvent({0}) - ProcessID: {1} ParentProcessID: {2}", processName, processId, parentProcessId);

            ProcessEventArgs eventArgs = new ProcessEventArgs(sessionId, processName, processId, parentProcessId);
            OnProcessStarted?.Invoke(this, eventArgs);
        }

        private void CreateProcessStopEvent(ManagementBaseObject baseObject)
        {
            int processId = Convert.ToInt32(baseObject.Properties["ProcessId"].Value);
            int parentProcessId = Convert.ToInt32(baseObject.Properties["ParentProcessId"].Value);
            int sessionId = Convert.ToInt32(baseObject.Properties["SessionId"].Value);
            string processName = (string)baseObject.Properties["Name"].Value;
            LoggerHelper.Debug("ProcessEventMonitor::CreateProcessStopEvent({0}) - ProcessID: {1} ParentProcessID: {2}", processName, processId, parentProcessId);

            ProcessEventArgs eventArgs = new ProcessEventArgs(sessionId, processName, processId, parentProcessId);
            OnProcessStopped?.Invoke(this, eventArgs);
        }
    }
}
