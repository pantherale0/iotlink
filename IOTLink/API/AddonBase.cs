using IOTLink.Engine;

namespace IOTLink.API
{
    /// <summary>
	/// Base application class used by both application interface and scripts.
	/// </summary>
    public abstract class AddonBase
    {
        /// <summary>
		/// Current application directory.
		/// </summary>
		protected string _currentPath;

        /// <summary>
        /// Current set of informations about this application.
        /// </summary>
        protected AddonInfo _addonInfo;

        /// <summary>
        /// AddonManager instance
        /// </summary>
        protected AddonManager _manager = AddonManager.GetInstance();

        /// <summary>
		/// Application constructor
		/// </summary>
		public AddonBase()
        {

        }

        /// <summary>
        /// Initialize the App
        /// </summary>
        public virtual void Init()
        {

        }

        /// <summary>
		/// Get the current application directory
		/// </summary>
		/// <returns>string containing the app directory</returns>
		public string GetCurrentPath()
        {
            return _currentPath;
        }

        /// <summary>
		/// Set the current app directory. Used internally.
		/// </summary>
		/// <param name="path">String containing the app directory</param>
		internal void SetCurrentPath(string path)
        {
            _currentPath = path;
        }

        /// <summary>
		/// Get the current <see cref="AddonInfo"/>
		/// </summary>
		internal AddonInfo GetAppInfo()
        {
            return _addonInfo;
        }

        /// <summary>
		/// Set the current <see cref="AddonInfo"/>.
		/// </summary>
		/// <param name="addonInfo"></param>
		internal void SetAddonInfo(AddonInfo addonInfo)
        {
            _addonInfo = addonInfo;
        }
    }
}
