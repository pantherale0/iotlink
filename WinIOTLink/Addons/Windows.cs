using WinIOTLink.API;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Helpers;

namespace WinIOTLink.Addons
{
    internal class Windows : AddonScript
    {
        public override void Init()
        {
            base.Init();

            _manager.SubscribeTopic(this, "shutdown", OnShutdownMessage);
            _manager.SubscribeTopic(this, "reboot", OnRebootMessage);
            _manager.SubscribeTopic(this, "logoff", OnLogoffMessage);
            _manager.SubscribeTopic(this, "lock", OnLockMessage);
            _manager.SubscribeTopic(this, "hibernate", OnHibernateMessage);
            _manager.SubscribeTopic(this, "suspend", OnSuspendMessage);
        }

        private void OnShutdownMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnShutdownMessage message received");
            WindowsHelper.Shutdown();
        }

        private void OnRebootMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnRebootMessage message received");
            WindowsHelper.Reboot();
        }

        private void OnLogoffMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnLogoffMessage message received");
            WindowsHelper.Logoff(e.Message.GetPayload());
        }

        private void OnLockMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnLockMessage message received");
            WindowsHelper.Lock(e.Message.GetPayload());
        }

        private void OnHibernateMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnHibernateMessage message received");
            WindowsHelper.Hibernate();
        }

        private void OnSuspendMessage(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Info("Windows", "OnSuspendMessage message received");
            WindowsHelper.Suspend();
        }
    }
}
