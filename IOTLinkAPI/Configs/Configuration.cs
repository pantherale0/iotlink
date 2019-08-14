using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IOTLinkAPI.Configs
{
    public class Configuration
    {
        private Dictionary<object, object> config;

        public Configuration(Dictionary<object, object> config)
        {
            this.config = config;
        }

        public object ReadConfigKey(string key, object defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValue;

            string[] keys = key.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (keys.Length == 0 || !config.ContainsKey(keys[0]))
                return defaultValue;

            var result = config[keys[0]];

            keys = keys.Skip(1).ToArray();
            foreach (string subKey in keys)
            {
                if ((result is Dictionary<object, object>) == false)
                    return defaultValue;

                var dict = (Dictionary<object, object>)result;
                if (!dict.ContainsKey(subKey))
                    return defaultValue;

                result = dict[subKey];
            }

            return result;
        }

        public Configuration GetValue(string key)
        {
            Dictionary<object, object> cfg = (Dictionary<object, object>)ReadConfigKey(key);
            if (cfg == null)
                return null;

            return new Configuration(cfg);
        }

        public string GetValue(string key, string defaultValue = null)
        {
            object result = ReadConfigKey(key, defaultValue);
            if (result == null)
                return defaultValue;

            return result.ToString();
        }

        public bool GetValue(string key, bool defaultValue = false)
        {
            object result = ReadConfigKey(key, defaultValue);
            return MathHelper.ToBoolean(result, defaultValue);
        }

        public int GetValue(string key, int defaultValue = 0)
        {
            object result = ReadConfigKey(key, defaultValue);
            return MathHelper.ToInteger(result, defaultValue);
        }

        public long GetValue(string key, long defaultValue = 0L)
        {
            object result = ReadConfigKey(key, defaultValue);
            return MathHelper.ToLong(result, defaultValue);
        }

        public double GetValue(string key, double defaultValue = 0d)
        {
            object result = ReadConfigKey(key, defaultValue);
            return MathHelper.ToDouble(result, defaultValue);
        }

        public float GetValue(string key, float defaultValue = 0f)
        {
            object result = ReadConfigKey(key, defaultValue);
            return MathHelper.ToFloat(result, defaultValue);
        }

        public int GetListCount(string key)
        {
            var listObj = ReadConfigKey(key, null);
            if ((listObj is List<object>) == false)
                return 0;

            return ((List<object>)listObj).Count;
        }

        public List<object> GetList(string key)
        {
            var listObj = ReadConfigKey(key, null);
            if ((listObj is List<object>) == false)
                return new List<object>();

            return ((List<object>)listObj);
        }

        public List<Configuration> GetConfigurationList(string key)
        {
            var listObj = ReadConfigKey(key, null);
            if ((listObj is List<object>) == false)
                return null;

            List<object> myList = (List<object>)listObj;
            List<Configuration> result = new List<Configuration>();
            foreach (var item in myList)
            {
                if ((item is Dictionary<object, object>) == false)
                    continue;

                result.Add(new Configuration((Dictionary<object, object>)item));
            }

            return result;
        }

        public Configuration GetConfigurationListItem(string key, int index)
        {
            var listObj = ReadConfigKey(key, null);
            if ((listObj is List<object>) == false)
                return null;

            List<object> myList = (List<object>)listObj;
            if (myList.Count <= index)
                return null;

            var item = myList[index];
            if ((item is Dictionary<object, object>) == false)
                return null;

            return new Configuration((Dictionary<object, object>)item);
        }
    }
}
