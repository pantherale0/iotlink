using IOTLinkAPI.Configs;
using System.IO;
using YamlDotNet.Serialization;

namespace IOTLinkAPI.Helpers
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
            string configText = PathHelper.GetFileText(path);
            if (string.IsNullOrWhiteSpace(configText))
                return default(T);

            StringReader Reader = new StringReader(configText);
            IDeserializer YAMLDeserializer = new DeserializerBuilder().Build();

            return YAMLDeserializer.Deserialize<T>(Reader);
        }

        public static ApplicationConfig GetEngineConfig(bool force = false)
        {
            if (_config != null)
            {
                if (!force)
                    return _config;

                _config = null;
            }


            string path = Path.Combine(PathHelper.ConfigPath(), "configuration.yaml");
            _config = GetConfig<ApplicationConfig>(path);
            return _config;
        }
    }
}
