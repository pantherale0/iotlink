namespace IOTLinkAPI.Helpers
{
    public class ServiceHelper
    {
        public static void CallInteractive(string[] args)
        {
            CallInteractive(string.Join(" ", args));
        }

        public static void CallInteractive(string args)
        {
            PlatformHelper.Run(PathHelper.BaseAppFullName(), args, PathHelper.BaseAppPath());
        }

        public static void ShowToast(string title, string message, string imageUrl = null)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                imageUrl = string.Empty;

            CallInteractive(string.Format("-toastTitle \"{0}\" -toastImage \"{1}\" --showMessage \"{2}\" ", title, imageUrl, message));
        }

        public static void GetScreenshot()
        {
            CallInteractive(string.Format("--screenshot \"{0}\"", "C:\\teste.png"));
        }
    }
}
