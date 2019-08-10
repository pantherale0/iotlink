using System;
using System.Collections.Generic;
using System.Globalization;
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
            return (string)ReadConfigKey(key, defaultValue);
        }

        public bool GetValue(string key, bool defaultValue = false)
        {
            try
            {
                object result = ReadConfigKey(key, defaultValue);
                return Convert.ToBoolean(result);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public int GetValue(string key, int defaultValue = 0)
        {
            try
            {
                object result = ReadConfigKey(key, defaultValue);
                return Convert.ToInt32(result);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public long GetValue(string key, long defaultValue = 0L)
        {
            try
            {
                object result = ReadConfigKey(key, defaultValue);
                return Convert.ToInt64(result);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public double GetValue(string key, double defaultValue = 0d)
        {
            try
            {
                object result = ReadConfigKey(key, defaultValue);
                return Convert.ToDouble(result);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public float GetValue(string key, float defaultValue = 0f)
        {
            try
            {
                string result = (string)ReadConfigKey(key, defaultValue);
                return float.Parse(result, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception)
            {
                return defaultValue;
            }
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
