namespace IOTLinkAPI.Platform.Windows
{
    internal class WindowsSessionInfo
    {
        public int SessionID { get; set; }

        public string UserName { get; set; }

        public string StationName { get; set; }

        public bool IsActive { get; set; }

        public bool IsUser(string user)
        {
            return UserName != null && user != null && string.Compare(UserName.Trim().ToLowerInvariant(), user.Trim().ToLowerInvariant()) == 0;
        }
    }
}
