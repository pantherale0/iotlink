using System.Text;

namespace WinIOTLink.Engine.MQTT
{
    public class MQTTMessage
    {
        public string Topic { get; set; }
        public string FullTopic { get; set; }
        public byte[] Payload { get; set; }
        public bool Retain { get; set; }
        public string ContentType { get; set; }

        public string GetPayload()
        {
            return Encoding.UTF8.GetString(Payload);
        }

        public void SetPayload(string message)
        {
            Payload = Encoding.UTF8.GetBytes(message);
        }
    }
}
