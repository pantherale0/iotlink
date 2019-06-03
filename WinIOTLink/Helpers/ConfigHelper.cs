using System;
using System.IO;
using WinIOTLink.Configs;
using YamlDotNet.Serialization;

namespace WinIOTLink.Helpers
{
    public static class ConfigHelper
    {
        private static ApplicationConfig _config;

        internal static ApplicationConfig GetApplicationConfig()
        {
            if (_config != null)
                return _config;

            LoggerHelper.Info("WinIOTLink", "Reading configuration.yaml");

            string path = PathHelper.DataPath() + "\\configuration.yaml";
            string ConfigText = File.ReadAllText(path);
            StringReader Reader = new StringReader(ConfigText);
            IDeserializer YAMLDeserializer = new DeserializerBuilder().Build();

            _config = YAMLDeserializer.Deserialize<ApplicationConfig>(Reader);
            return _config;
        }
    }
}
