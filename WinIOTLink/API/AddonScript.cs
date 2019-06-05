using System;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Engine.System;
using static WinIOTLink.Engine.MQTT.MQTTHandlers;

namespace WinIOTLink.API
{
    /// <summary>
	/// Base application class.
	/// This class should be inherited by any application that needs to run in background.
	/// This class is a simplified version of Script class of SHDN (As SHDN do not allow us to register new scripts on runtime).
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

        internal void Raise_OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            if (OnSessionChangeHandler != null)
                OnSessionChangeHandler(this, e);
        }

        internal void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            if (OnMQTTConnectedHandler != null)
                OnMQTTConnectedHandler(sender, e);
        }

        internal void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            if (OnMQTTDisconnectedHandler != null)
                OnMQTTDisconnectedHandler(sender, e);
        }

        internal void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            if (OnMQTTMessageReceivedHandler != null)
                OnMQTTMessageReceivedHandler(sender, e);
        }

        internal void Raise_OnConfigReloadHandler(object sender, EventArgs e)
        {
            if (OnConfigReloadHandler != null)
                OnConfigReloadHandler(sender, e);
        }
    }
}
