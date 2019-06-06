using System.IO;
using IOTLink.Configs;
using YamlDotNet.Serialization;

namespace IOTLink.Helpers
{
    public static class ConfigHelper
    {
        private static ApplicationConfig _config;

        internal static ApplicationConfig GetEngineConfig(bool force = false)
        {
            if (_config != null && !force)
                return _config;

            string path = Path.Combine(PathHelper.ConfigPath(), "configuration.yaml");
            string ConfigText = PathHelper.GetFileText(path);
            StringReader Reader = new StringReader(ConfigText);
            IDeserializer YAMLDeserializer = new DeserializerBuilder().Build();

            _config = YAMLDeserializer.Deserialize<ApplicationConfig>(Reader);
            return _config;
        }
    }
}
