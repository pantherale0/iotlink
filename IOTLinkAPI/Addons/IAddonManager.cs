using IOTLinkAPI.Addons;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLink.Addons
{
    public interface IAddonManager
    {
        void SubscribeTopic(AddonScript sender, string topic, MQTTMessageEventHandler msgHandler);

        bool HasSubscription(AddonScript sender, string topic);

        void RemoveSubscription(AddonScript sender, string topic);

        void PublishMessage(AddonScript sender, string topic, string message);

        void PublishMessage(AddonScript sender, string topic, byte[] message);

    }
}
