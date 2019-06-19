﻿using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkAPI.Addons
{
    public interface IAddonServiceManager : IAddonManager
    {
        void SubscribeTopic(ServiceAddon sender, string topic, MQTTMessageEventHandler msgHandler);

        bool HasSubscription(ServiceAddon sender, string topic);

        void RemoveSubscription(ServiceAddon sender, string topic);

        void PublishMessage(ServiceAddon sender, string topic, string message);

        void PublishMessage(ServiceAddon sender, string topic, byte[] message);

        void SendAgentRequest(ServiceAddon sender, dynamic addonData, string username = null);
    }
}