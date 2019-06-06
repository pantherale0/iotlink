using System.Text;

namespace IOTLink.Helpers
{
    public static class MQTTHelper
    {
        /// <summary>
        /// Remove non-wanted characters from the node name
        /// </summary>
        /// <param name="name">String containing the source name</param>
        /// <returns>String containing the sanitized name</returns>
        public static string SanitizeNodeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return StringHelper.RemoveDiacritics(name)
                .Replace(" ", "_")
                .Replace("\\", "")
                .Trim();
        }

        /// <summary>
        /// Remove non-wanted characters from the MQTT topic path
        /// </summary>
        /// <param name="name">String containing the MQTT topic path</param>
        /// <returns>String containing the MQTT topic path</returns>
        public static string SanitizeTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return string.Empty;

            topic = topic.Replace("\\\\", "\\").Trim();
            string[] nodes = topic.Split('/');

            StringBuilder sb = new StringBuilder();
            foreach (string node in nodes)
            {
                sb.Append(SanitizeNodeName(node));
                sb.Append('/');
            }

            sb.Length--;
            return sb.ToString().ToLowerInvariant();
        }
    }
}
