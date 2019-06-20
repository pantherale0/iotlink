using IOTLinkAPI.Helpers;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace IOTLinkAgent.Agent.Notifications
{
    class NotificationManager
    {
        private const string APP_ID = "IOT Link";

        private static NotificationManager _instance;

        public static NotificationManager GetInstance()
        {
            if (_instance == null)
                _instance = new NotificationManager();

            return _instance;
        }

        private NotificationManager()
        {
            LoggerHelper.Trace("NotificationManager instance created.");
        }

        public void ShowNotification(string message, string imageUrl = null)
        {
            ToastTemplateType toastTemplateType = string.IsNullOrWhiteSpace(imageUrl) ? ToastTemplateType.ToastText01 : ToastTemplateType.ToastImageAndText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplateType);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(message));

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = imageUrl;
            }

            ShowToast(toastXml);
        }

        public void ShowNotification(string title, string message, string imageUrl = null)
        {
            ToastTemplateType toastTemplateType = string.IsNullOrWhiteSpace(imageUrl) ? ToastTemplateType.ToastText02 : ToastTemplateType.ToastImageAndText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplateType);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = imageUrl;
            }

            ShowToast(toastXml);
        }

        public void ShowNotification(string title, string messageLine1, string messageLine2, string imageUrl = null)
        {
            ToastTemplateType toastTemplateType = string.IsNullOrWhiteSpace(imageUrl) ? ToastTemplateType.ToastText04 : ToastTemplateType.ToastImageAndText04;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplateType);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(messageLine1));
            stringElements[2].AppendChild(toastXml.CreateTextNode(messageLine2));

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = imageUrl;
            }

            ShowToast(toastXml);
        }

        private void ShowToast(XmlDocument toastXml)
        {
            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Failed += ToastFailed;

            // Show the toast.
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        private void ToastActivated(ToastNotification sender, object e)
        {
            LoggerHelper.Trace("Toast Activated");
        }

        private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
        {
            LoggerHelper.Trace("Toast Dismissed: {0}", e.Reason.ToString());

            switch (e.Reason)
            {
                case ToastDismissalReason.ApplicationHidden:
                    break;
                case ToastDismissalReason.UserCanceled:
                    break;
                case ToastDismissalReason.TimedOut:
                    break;
            }
        }

        private void ToastFailed(ToastNotification sender, ToastFailedEventArgs e)
        {
            LoggerHelper.Error("Toast Failed: {0}", e.ErrorCode.ToString());
        }
    }
}
