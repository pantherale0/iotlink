using System;
using System.IO;
using IOTLink.Configs;
using YamlDotNet.Serialization;

namespace IOTLink.Helpers
{
    public static class ConfigHelper
    {
        private static ApplicationConfig _config;

        /// <summary>
        /// Read a configuration YAML file.
        /// </summary>
        /// <typeparam name="T">Configuration class</typeparam>
        /// <param name="path">Configuration file path</param>
        /// <returns></returns>
        public static T GetConfig<T>(string path)
        {
            string ConfigText = PathHelper.GetFileText(path);
            StringReader Reader = new StringReader(ConfigText);
            IDeserializer YAMLDeserializer = new DeserializerBuilder().Build();

            return YAMLDeserializer.Deserialize<T>(Reader);
        }

        internal static ApplicationConfig GetEngineConfig(bool force = false)
        {
            if (_config != null && !force)
                return _config;

            string path = Path.Combine(PathHelper.ConfigPath(), "configuration.yaml");
            _config = GetConfig<ApplicationConfig>(path);
            return _config;
        }
    }
}
