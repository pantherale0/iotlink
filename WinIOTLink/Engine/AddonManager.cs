using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinIOTLink.API;
using WinIOTLink.Configs;
using WinIOTLink.Engine.MQTT;
using WinIOTLink.Helpers;
using WinIOTLink.Loaders;

namespace WinIOTLink.Engine
{
    public class AddonManager
    {
        private static AddonManager _instance;
        private Dictionary<string, AddonInfo> _addons = new Dictionary<string, AddonInfo>();

        public static AddonManager GetInstance()
        {
            if (_instance == null)
                _instance = new AddonManager();

            return _instance;
        }

        private AddonManager()
        {

        }

        /// <summary>
		/// Add an <see cref="AddonInfo"/> to the current loaded list.
		/// </summary>
		/// <param name="id">String containing the application ID.</param>
		/// <param name="AddonInfo"><see cref="AddonInfo"/> structure.</param>
		public void AddAddon(string id, AddonInfo addonInfo)
        {
            if (id == null)
                return;

            id = id.Trim().ToLowerInvariant();
            LoggerHelper.Info("AddonManager", "Loading addon: " + id);
            _addons.Add(id, addonInfo);
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
		/// Search into app directory and load all enabled and valid applications.
		/// </summary>
		public void LoadAddons()
        {
            List<string> dirs = new List<string>(Directory.EnumerateDirectories(PathHelper.AddonsPath()));

            this._addons.Clear();

            LoggerHelper.Error("AddonManager", String.Format("Loading {0} addons", dirs.Count));
            foreach (var dir in dirs)
            {
                string DirName = dir.Substring(dir.LastIndexOf("/") + 1);
                AddonInfo addonInfo = new AddonInfo();

                if (this.LoadAppSettings(DirName, ref addonInfo) == true && addonInfo.Enabled == true)
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
        private bool LoadAppSettings(string DirName, ref AddonInfo addonInfo)
        {
            AddonConfig config = AddonConfig.GetInstance(DirName);
            if (!config.Load())
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

            LoggerHelper.Error("AddonManager", "Addon loaded: " + addonInfo.AddonName);
            return true;
        }

        internal void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            List<AddonInfo> addons = this.GetAppList();
            foreach (AddonInfo addonInfo in addons)
            {
                addonInfo.ScriptClass?.Raise_OnMQTTConnected(sender, e);
            }
        }

        internal void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            List<AddonInfo> addons = this.GetAppList();
            foreach (AddonInfo addonInfo in addons)
            {
                addonInfo.ScriptClass?.Raise_OnMQTTDisconnected(sender, e);
            }
        }

        internal void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            List<AddonInfo> addons = this.GetAppList();
            foreach (AddonInfo addonInfo in addons)
            {
                addonInfo.ScriptClass?.Raise_OnMQTTMessageReceived(sender, e);
            }
        }
    }
}
