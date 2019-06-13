namespace IOTLinkAPI.Platform
{
    public class RunInfo
    {
        public string Application { get; set; }

        public string CommandLine { get; set; }

        public string WorkingDir { get; set; }

        public string UserName { get; set; }

        public bool Visible { get; set; } = true;

        public bool FallbackToFirstActiveUser { get; set; } = false;
    }
}
