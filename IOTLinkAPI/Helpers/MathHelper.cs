using System;

namespace IOTLinkAPI.Helpers
{
    public abstract class MathHelper
    {

        public static double BytesToMegabytes(long bytes)
        {
            return Math.Round(bytes / (1024f * 1024f), 2);
        }

        public static double BytesToGigabytes(long bytes)
        {
            return Math.Round(bytes / (1024f * 1024f * 1024f), 2);
        }

        public static double BytesToTerabytes(long bytes)
        {
            return Math.Round(bytes / (1024f * 1024f * 1024f * 1024f), 2);
        }

        public static double MegabytesToGigabytes(long megabytes)
        {
            return Math.Round(megabytes / 1024f, 2);
        }

        public static double MegabytesToTerabytes(long megabytes)
        {
            return Math.Round(megabytes / (1024f * 1024f), 2);
        }
    }
}
