using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using System;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkAPI.Addons
{
    /// <summary>
    /// Service Addon Base.
    /// This class should be inherited by any service addon that needs to run in background.
    /// This will be run on the Windows Service.
    /// </summary>
    /// <seealso cref="AddonBase"/>
    public abstract class ServiceAddon : AddonBase
    {
        public event MQTTEventHandler OnMQTTConnectedHandler;
        public event MQTTEventHandler OnMQTTDisconnectedHandler;
        public event MQTTMessageEventHandler OnMQTTMessageReceivedHandler;
        public event SessionChangeHandler OnSessionChangeHandler;
        public event ConfigReloadedHandler OnConfigReloadHandler;

        public delegate void SessionChangeHandler(object sender, SessionChangeEventArgs e);
        public delegate void ConfigReloadedHandler(object sender, ConfigReloadEventArgs e);

        public void Raise_OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            OnSessionChangeHandler?.Invoke(this, e);
        }

        public void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            OnMQTTConnectedHandler?.Invoke(sender, e);
        }

        public void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            OnMQTTDisconnectedHandler?.Invoke(sender, e);
        }

        public void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            OnMQTTMessageReceivedHandler?.Invoke(sender, e);
        }

        public void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            OnConfigReloadHandler?.Invoke(sender, e);
        }
    }
}
