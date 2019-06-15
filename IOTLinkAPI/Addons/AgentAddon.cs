using IOTLinkAPI.Platform.Events;

namespace IOTLinkAPI.Addons
{
    /// <summary>
    /// Agent Addon Base.
    /// This class should be inherited by any agent addon that needs to run in background.
    /// This will be run on the IOT Link Agents.
    /// </summary>
    /// <seealso cref="AddonBase"/>
    public abstract class AgentAddon : AddonBase
    {
        public event ConfigReloadedHandler OnConfigReloadHandler;
        public event AgentRequestHandler OnAgentRequestHandler;

        public delegate void ConfigReloadedHandler(object sender, ConfigReloadEventArgs e);
        public delegate void AgentRequestHandler(object sender, AgentAddonRequestEventArgs e);

        public IAddonAgentManager GetManager()
        {
            return (IAddonAgentManager)_manager;
        }

        public void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            OnConfigReloadHandler?.Invoke(sender, e);
        }

        public void Raise_OnAgentResponse(object sender, AgentAddonRequestEventArgs e)
        {
            OnAgentRequestHandler?.Invoke(sender, e);
        }
    }
}
