using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using System;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkAPI.Addons
{
    /// <summary>
	/// Base Addon class.
	/// This class should be inherited by any addon that needs to run in background.
	/// </summary>
	/// <seealso cref="AddonBase"/>
    public abstract class AddonScript : AddonBase
    {
        public event MQTTEventHandler OnMQTTConnectedHandler;
        public event MQTTEventHandler OnMQTTDisconnectedHandler;
        public event MQTTMessageEventHandler OnMQTTMessageReceivedHandler;
        public event SessionChangeHandler OnSessionChangeHandler;
        public event ConfigReloadedHandler OnConfigReloadHandler;

        public delegate void SessionChangeHandler(Object sender, SessionChangeEventArgs e);
        public delegate void ConfigReloadedHandler(Object sender, EventArgs e);

        public void Raise_OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            if (OnSessionChangeHandler != null)
                OnSessionChangeHandler(this, e);
        }

        public void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            if (OnMQTTConnectedHandler != null)
                OnMQTTConnectedHandler(sender, e);
        }

        public void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            if (OnMQTTDisconnectedHandler != null)
                OnMQTTDisconnectedHandler(sender, e);
        }

        public void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            if (OnMQTTMessageReceivedHandler != null)
                OnMQTTMessageReceivedHandler(sender, e);
        }

        public void Raise_OnConfigReloadHandler(object sender, EventArgs e)
        {
            if (OnConfigReloadHandler != null)
                OnConfigReloadHandler(sender, e);
        }
    }
}
