using System;

namespace IOTLinkAPI.Platform.Events.MQTT
{
    public abstract class MQTTHandlers
    {
        public delegate void MQTTEventHandler(Object sender, MQTTEventEventArgs e);
        public delegate void MQTTMessageEventHandler(Object sender, MQTTMessageEventEventArgs e);
    }
}
