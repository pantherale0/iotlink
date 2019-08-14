using System;

namespace IOTLinkAPI.Helpers
{
    public abstract class UnitsHelper
    {
        public static double BytesToKilobytes(long bytes)
        {
            return Math.Round(bytes / 1024f, 2);
        }

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

        public static double TerabytesToBytes(long terabytes)
        {
            return Math.Round(terabytes * (1024f * 1024f * 1024f * 1024f), 2);
        }

        public static double GigabytesToBytes(long gigabytes)
        {
            return Math.Round(gigabytes * (1024f * 1024f * 1024f), 2);
        }

        public static double MegabytesToBytes(long megabytes)
        {
            return Math.Round(megabytes * (1024f * 1024f), 2);
        }

        public static double KilobytesToBytes(long kilobytes)
        {
            return Math.Round(kilobytes * 1024f, 2);
        }
    }
}
