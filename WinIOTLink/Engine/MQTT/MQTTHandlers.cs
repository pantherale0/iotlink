using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIOTLink.Engine.MQTT
{
    public abstract class MQTTHandlers
    {
        public delegate void MQTTEventHandler(Object sender, MQTTEventEventArgs e);
        public delegate void MQTTMessageEventHandler(Object sender, MQTTMessageEventEventArgs e);
    }
}
