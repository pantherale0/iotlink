using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinIOTLink.Addons;
using WinIOTLink.API;
using WinIOTLink.Configs;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Engine.System;
using WinIOTLink.Helpers;
using WinIOTLink.Loaders;
using static WinIOTLink.Engine.MQTT.MQTTHandlers;

namespace WinIOTLink.Engine
{
    public class AddonManager
    {
        private static AddonManager _instance;
        private Dictionary<string, AddonInfo> _addons = new Dictionary<string, AddonInfo>();
        private Dictionary<string, MQTTMessageEventHandler> _topics = new Dictionary<string, MQTTMessageEventHandler>();

        public static AddonManager GetInstance()
        {
            if (_instance == null)
                _instance = new AddonManager();

            return _instance;
        }

        private AddonManager()
        {

        }

        public void SubscribeTopic(AddonScript sender, string topic, MQTTMessageEventHandler msgHandler)
        {
            if (sender == null || string.IsNullOrWhiteSpace(topic) || HasSubscription(sender, topic))
                return;

            string addonTopic = GetAddonTopic(sender, topic);
            _topics.Add(addonTopic, msgHandler);

            LoggerHelper.Info("AddonManager", string.Format("Addon {0} has subscribed to topic {1}", sender.GetAppInfo().AddonId, addonTopic));
        }

        public bool HasSubscription(AddonScript sender, string topic)
        {
            if (sender == null)
                return false;

            string addonTopic = GetAddonTopic(sender, topic);
            return _topics.ContainsKey(addonTopic);
        }

        public void RemoveSubscription(AddonScript sender, string topic)
        {
            if (sender == null || !HasSubscription(sender, topic))
                return;

            string addonTopic = GetAddonTopic(sender, topic);
            _topics.Remove(addonTopic);

            LoggerHelper.Info("AddonManager", string.Format("Addon {0} has removed subscription to topic {1}", sender.GetAppInfo().AddonId, addonTopic));
        }

        public void PublishMessage(AddonScript sender, string topic, string message)
        {
            if (sender == null || string.IsNullOrWhiteSpace(topic))
                return;

            string addonTopic = GetAddonTopic(sender, topic);
            MQTTClient.GetInstance().PublishMessage(addonTopic, message);
        }

        public void PublishMessage(AddonScript sender, string topic, byte[] message)
        {
            if (sender == null || string.IsNullOrWhiteSpace(topic))
                return;

            string addonTopic = GetAddonTopic(sender, topic);
            MQTTClient.GetInstance().PublishMessage(addonTopic, message);
        }

        /// <summary>
		/// Check if any application exists with the given ID.
		/// </summary>
		/// <param name="id">String containing the application ID.</param>
		/// <returns>True if the app exists, false otherwise.</returns>
		public bool AddonExists(string id)
        {
            if (id == null)
                return false;

            id = id.Trim().ToLowerInvariant();

            if (_addons.ContainsKey(id))
                return true;

            return false;
        }

        /// <summary>
        /// Get an <see cref="AddonInfo"/> by its Id.
        /// </summary>
        /// <param name="id">String containing the application ID</param>
        /// <returns><see cref="AddonInfo"/> if found. Blank structure otherwise.</returns>
        public AddonInfo GetAppById(string id)
        {
            if (id == null)
                return null;

            id = id.Trim().ToLowerInvariant();

            if (!this.AddonExists(id))
                return null;

            return _addons[id];
        }

        /// <summary>
        /// Get a list of all loaded applications.
        /// </summary>
        /// <returns>A list containing all loaded <see cref="AddonInfo">applications</see>.</returns>
        public List<AddonInfo> GetAppList()
        {
            return this._addons.Values.ToList();
        }

        /// <summary>
		/// Add an <see cref="AddonInfo"/> to the current loaded list.
		/// </summary>
		/// <param name="id">String containing the application ID.</param>
		/// <param name="AddonInfo"><see cref="AddonInfo"/> structure.</param>
		internal void AddAddon(string id, AddonInfo addonInfo)
        {
            if (id == null)
                return;

            id = id.Trim().ToLowerInvariant();
            LoggerHelper.Info("AddonManager", "Loading addon: " + id);
            _addons.Add(id, addonInfo);
        }

        /// <summary>
		/// Search into app directory and load all enabled and valid applications.
		/// </summary>
		internal void LoadAddons()
        {
            this._addons.Clear();
            this.LoadInternalAddons();
            this.LoadExternalAddons();
        }

        /// <summary>
		/// Search into app directory and load all enabled and valid applications.
		/// </summary>
		private void LoadInternalAddons()
        {
            AddonScript[] internalAddons = new AddonScript[]
            {
                new WindowsMonitor(),
                new Windows()
            };

            LoggerHelper.Info("AddonManager", string.Format("Loading {0} internal addons", internalAddons.Length));
            foreach (AddonScript addon in internalAddons)
            {
                AddonInfo addonInfo = new AddonInfo();
                addonInfo.AddonName = addon.GetType().Name;
                addonInfo.AddonPath = PathHelper.BasePath();
                addonInfo.AddonFile = String.Empty;
                addonInfo.Internal = true;
                addonInfo.AddonId = addon.GetType().Name;
                addonInfo.ScriptClass = addon;

                if (addonInfo.ScriptClass != null)
                {
                    addonInfo.ScriptClass.SetAddonInfo(addonInfo);
                    addonInfo.ScriptClass.SetCurrentPath(addonInfo.AddonPath);
                    addonInfo.ScriptClass.Init();
                }

                this.AddAddon(addonInfo.AddonId, addonInfo);
            }
        }

        /// <summary>
		/// Search into app directory and load all enabled and valid applications.
		/// </summary>
		private void LoadExternalAddons()
        {
            string addonsPath = PathHelper.AddonsPath();

            // Create addons directory.
            if (!Directory.Exists(addonsPath))
            {
                LoggerHelper.Warn("AddonManager", string.Format("Addons directory doesn't exists. Creating {0}", addonsPath));
                Directory.CreateDirectory(addonsPath);
                return;
            }

            List<string> dirs = new List<string>(Directory.EnumerateDirectories(addonsPath));
            LoggerHelper.Info("AddonManager", string.Format("Loading {0} external addons", dirs.Count));
            foreach (var dir in dirs)
            {
                string DirName = dir.Substring(dir.LastIndexOf("/") + 1);
                AddonInfo addonInfo = new AddonInfo();

                if (this.LoadSettingsFromDir(DirName, ref addonInfo) == true && addonInfo.Enabled == true)
                {
                    if (!AssemblyLoader.LoadAppAssembly(ref addonInfo))
                        continue;

                    if (addonInfo.ScriptClass != null)
                    {
                        addonInfo.ScriptClass.SetAddonInfo(addonInfo);
                        addonInfo.ScriptClass.SetCurrentPath(addonInfo.AddonPath);
                        addonInfo.ScriptClass.Init();
                    }

                    this.AddAddon(addonInfo.AddonId, addonInfo);
                }
            }
        }

        /// <summary>
        /// Load the application informations
        /// </summary>
        /// <param name="DirName">Directory name containing the configuration file.</param>
        /// <param name="addonInfo"><see cref="AppInfo"/> structure to be loaded.</param>
        /// <returns>True if valid configuration file found, false otherwise.</returns>
        private bool LoadSettingsFromDir(string DirName, ref AddonInfo addonInfo)
        {
            AddonConfig config = AddonConfig.GetInstance(DirName);
            if (!config.Load())
                return false;

            return LoadAppSettings(config, ref addonInfo);
        }

        private bool LoadAppSettings(AddonConfig config, ref AddonInfo addonInfo)
        {
            if (config == null)
                return false;

            addonInfo.Settings = config;
            addonInfo.AddonId = config.AddonId;
            addonInfo.Enabled = config.Enabled;
            addonInfo.AddonPath = config.AddonPath;
            addonInfo.AddonName = config.AddonName;
            addonInfo.AddonFile = config.AddonFile;

            /**
			 * Check for a valid AppID
			 */
            if (string.IsNullOrWhiteSpace(addonInfo.AddonId) || !StringHelper.IsValidID(addonInfo.AddonId))
            {
                LoggerHelper.Error("AddonManager", "Invalid App: " + addonInfo.AddonName);
                LoggerHelper.Error("AddonManager", "App unique id is missing or is invalid (only a-zA-Z0-9_ characters allowed).");
                LoggerHelper.Error("AddonManager", "App disabled.");
                addonInfo.Enabled = false;
            }

            /**
			 * Check for a valid AppName
			 */
            if (string.IsNullOrWhiteSpace(addonInfo.AddonName))
            {
                LoggerHelper.Error("AddonManager", "Invalid App: " + addonInfo.AddonName);
                LoggerHelper.Error("AddonManager", "App name is missing.");
                LoggerHelper.Error("AddonManager", "App disabled.");
                addonInfo.Enabled = false;
            }

            /**
			 * Check app compatibility
			 */
            if (!AssemblyHelper.CheckAssemblyVersion(config.MinApiVersion, config.MaxApiVersion))
            {
                LoggerHelper.Error("AddonManager", "Incompatible App found: " + addonInfo.AddonName);
                LoggerHelper.Error("AddonManager", "CurrentVersion: " + AssemblyHelper.GetCurrentVersion());
                LoggerHelper.Error("AddonManager", "MinVersion: " + config.MinApiVersion + ", MaxVersion: " + config.MaxApiVersion);
                LoggerHelper.Error("AddonManager", "App disabled.");
                addonInfo.Enabled = false;
            }

            LoggerHelper.Info("AddonManager", "Addon configuration loaded: " + addonInfo.AddonName);
            return true;
        }

        internal string GetAddonTopic(AddonScript addon, string topic)
        {
            if (addon == null)
                return string.Empty;

            return MQTTHelper.SanitizeTopic(string.Format("{0}/{1}", addon.GetAppInfo().AddonId, topic));
        }

        internal void Raise_OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            List<AddonInfo> addons = this.GetAppList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ScriptClass != null)
                    addonInfo.ScriptClass.Raise_OnSessionChange(sender, e);
            }
        }

        internal void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            List<AddonInfo> addons = this.GetAppList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ScriptClass != null)
                    addonInfo.ScriptClass.Raise_OnMQTTConnected(sender, e);
            }
        }

        internal void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            List<AddonInfo> addons = this.GetAppList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ScriptClass != null)
                    addonInfo.ScriptClass.Raise_OnMQTTDisconnected(sender, e);
            }
        }

        internal void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            if (_topics.ContainsKey(e.Message.Topic))
                _topics[e.Message.Topic](sender, e);
        }
    }
}
