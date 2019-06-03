using System;
using System.Collections.Generic;
using System.IO;
using WinIOTLink.Helpers;
using YamlDotNet.RepresentationModel;

namespace WinIOTLink.Configs
{
    /// <summary>
	/// Handle the configurations of an App.
	/// </summary>
    public class AddonConfig
    {
        /// <summary>
		/// Application Unique ID
		/// </summary>
		public string AddonId;

        /// <summary>
        /// Application Name
        /// </summary>
        public string AddonName;

        /// <summary>
        /// Application DLL file.
        /// </summary>
        public string AddonFile;

        /// <summary>
        /// Application Directory Path
        /// </summary>
        public string AddonPath;

        /// <summary>
        /// Minimum API version to run this application.
        /// </summary>
        public string MinApiVersion;

        /// <summary>
        /// Maximum API version to run this application.
        /// </summary>
        public string MaxApiVersion;

        /// <summary>
        /// Define if the app is enabled or not.
        /// </summary>
        public bool Enabled;

        private static Dictionary<string, AddonConfig> _configs = new Dictionary<string, AddonConfig>();

        /// <summary>
        /// Get a valid instance of this class or create a new one if necessary.
        /// </summary>
        /// <param name="dirName">String containing the directory name of the App to be read.</param>
        /// <returns>AppConfig instance</returns>
        public static AddonConfig GetInstance(string dirName)
        {
            if (!_configs.ContainsKey(dirName))
                _configs[dirName] = new AddonConfig(dirName);

            return _configs[dirName];
        }

        private AddonConfig(string dirName)
        {
            this.AddonPath = PathHelper.AddonsPath() + "\\" + dirName;
        }

        /// <summary>
        /// Load the configuration file if found.
        /// </summary>
        /// <returns>True when successful, false otherwise.</returns>
        public bool Load()
        {
            string filename = this.AddonPath + "/config.yaml";
            if (!File.Exists(filename))
                return false;

            try
            {
                var input = new StringReader(File.ReadAllText(filename));
                var yaml = new YamlStream();
                yaml.Load(input);

                var root = (YamlMappingNode)yaml.Documents[0].RootNode;
                this.AddonId = ((YamlScalarNode)root.Children[new YamlScalarNode("id")]).Value;
                this.AddonName = ((YamlScalarNode)root.Children[new YamlScalarNode("name")]).Value;
                this.AddonFile = ((YamlScalarNode)root.Children[new YamlScalarNode("file")]).Value;
                this.Enabled = Convert.ToBoolean(((YamlScalarNode)root.Children[new YamlScalarNode("enabled")]).Value);
                this.MinApiVersion = ((YamlScalarNode)root.Children[new YamlScalarNode("minApiVersion")]).Value;
                this.MaxApiVersion = ((YamlScalarNode)root.Children[new YamlScalarNode("maxApiVersion")]).Value;
                return true;
            }
            catch (Exception) { }

            return false;
        }
    }
}
