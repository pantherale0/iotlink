using System.Text;

namespace WinIOTLink.Helpers
{
    public static class MQTTHelper
    {
        public static object String { get; internal set; }

        public static string SanitizeNodeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return StringHelper.RemoveDiacritics(name)
                .Replace(" ", "_")
                .Replace("\\", "")
                .Trim()
                .ToLowerInvariant();
        }

        public static string SanitizeTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return string.Empty;

            topic = topic.Replace("\\\\", "\\").Trim().ToLowerInvariant();
            string[] nodes = topic.Split('/');

            StringBuilder sb = new StringBuilder();
            foreach (string node in nodes)
            {
                sb.Append(SanitizeNodeName(node));
                sb.Append('/');
            }

            sb.Length--;
            return sb.ToString();
        }
    }
}
