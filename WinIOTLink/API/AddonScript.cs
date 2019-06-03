using WinIOTLink.Engine.MQTT;
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
        public event MQTTEventHandler OnMQTTConnected;
        public event MQTTEventHandler OnMQTTDisconnected;
        public event MQTTMessageEventHandler OnMQTTMessageReceived;

        internal void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            if (OnMQTTConnected != null)
                OnMQTTConnected(sender, e);
        }

        internal void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            if (OnMQTTDisconnected != null)
                OnMQTTDisconnected(sender, e);
        }

        internal void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            if (OnMQTTMessageReceived != null)
                OnMQTTMessageReceived(sender, e);
        }
    }
}
