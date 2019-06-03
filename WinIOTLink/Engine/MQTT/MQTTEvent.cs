using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIOTLink.Engine.MQTT
{
    public class MQTTEvent
    {
        public MQTTEventType EventType { get; set; }
        public MQTTMessage Message { get; set; }
        public object EventArg { get; set; }

        public MQTTEvent(MQTTEventType type)
        {
            EventType = type;
        }

        public MQTTEvent(MQTTEventType type, object arg)
        {
            EventType = type;
            EventArg = arg;
        }

        public MQTTEvent(MQTTEventType type, MQTTMessage msg, object arg)
        {
            EventType = type;
            EventArg = arg;
            Message = msg;
        }

        public enum MQTTEventType
        {
            Connect,
            Disconnect,
            MessageReceived,
            MessageSentSuccess,
            MessageSentFailure
        }
    }
}
