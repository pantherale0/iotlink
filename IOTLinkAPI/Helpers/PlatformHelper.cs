﻿using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Helpers
{
    public static class PlatformHelper
    {
        /// <summary>
        /// Return the machine name containing the domain is necessary
        /// </summary>
        /// <returns>String</returns>
        public static string GetFullMachineName()
        {
            string domainName = Environment.UserDomainName;
            string computerName = Environment.MachineName;
            if (domainName.Equals(computerName))
                return computerName;

            return string.Format("{0}\\{1}", domainName, computerName);
        }

        /// <summary>
        /// Return the username from the sessionId
        /// </summary>
        /// <param name="sessionId">Integer</param>
        /// <returns>String</returns>
        public static string GetUsername(int sessionId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return WindowsAPI.GetUsername(sessionId);

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Return the username from the current session
        /// </summary>
        /// <returns>String</returns>
        public static string GetCurrentUsername()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return WindowsAPI.GetCurrentUsername();

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Execute a system shutdown
        /// </summary>
        /// <param name="force">Boolean indicating if the call should be flagged as forced</param>
        public static void Shutdown(bool force = false)
        {
            LoggerHelper.Debug("Executing {0} system shutdown.", force ? "forced" : "normal");
            string filename = "shutdown";
            string args = null;

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                args = force ? "-s -f -t 0" : "-s -t 0";

            // Linux or OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                args = "-h now";

            Process.Start(filename, args);
        }

        /// <summary>
        /// Execute a system reboot
        /// </summary>
        /// <param name="force">Boolean indicating if the call should be flagged as forced</param>
        public static void Reboot(bool force = false)
        {
            LoggerHelper.Debug("Executing {0} system reboot.", force ? "forced" : "normal");
            string filename = "shutdown";
            string args = null;

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                args = force ? "-r -f -t 0" : "-r -t 0";

            // Linux or OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                args = "-r -h now";

            Process.Start(filename, args);
        }

        /// <summary>
        /// Puts the system into a hibernate state if possible
        /// </summary>
        public static void Hibernate()
        {
            LoggerHelper.Debug("Executing system hibernation.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                WindowsAPI.Hibernate();

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Puts the system into a suspended state if possible
        /// </summary>
        public static void Suspend()
        {
            LoggerHelper.Debug("Executing system suspend.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                WindowsAPI.Suspend();

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Logoff the user from the system
        /// </summary>
        /// <param name="username">User which needs be logged-off</param>
        public static void Logoff(string username)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            if (string.IsNullOrWhiteSpace(username))
            {
                LoggerHelper.Debug("Executing Logoff on all users");
                WindowsAPI.LogoffAll();
            }
            else
            {
                LoggerHelper.Debug(string.Format("Executing Logoff on user {0}", username));
                WindowsAPI.LogOffUser(username);
            }
        }

        /// <summary>
        /// Lock the user session from the system
        /// </summary>
        /// <param name="username">User which needs be its session locked</param>
        public static void Lock(string username)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            if (string.IsNullOrWhiteSpace(username))
            {
                LoggerHelper.Debug("Locking all users sessions");
                WindowsAPI.LockAll();
            }
            else
            {
                LoggerHelper.Debug(string.Format("Locking {0} user session", username));
                WindowsAPI.LockUser(username);
            }
        }

        /// <summary>
        /// Execute an application
        /// </summary>
        /// <param name="runInfo">Application information</param>
        public static void Run(RunInfo runInfo)
        {
            if (!string.IsNullOrWhiteSpace(runInfo.CommandLine))
                runInfo.CommandLine = string.Format("{0} {1}", Path.GetFileName(runInfo.Application), runInfo.CommandLine);
            else
                runInfo.CommandLine = Path.GetFileName(runInfo.Application);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            LoggerHelper.Debug(
                "Run - Command: {0} Args: {1} WorkingDir: {2} User: {3} Visible: {4} FallBack: {5}",
                runInfo.Application, runInfo.CommandLine, runInfo.WorkingDir, runInfo.Username, runInfo.Visible, runInfo.Fallback
            );

            WindowsAPI.Run(runInfo);
        }

        /// <summary>
        /// Return a <see cref="MemoryInfo"/> object with all current memory information.
        /// </summary>
        /// <returns><see cref="MemoryInfo"/> object</returns>
        public static MemoryInfo GetMemoryInformation()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.GetMemoryInformation();
        }

        /// <summary>
        /// Set primary audio device volume mute flag
        /// </summary>
        /// <param name="mute">Boolean indicating the desired mute flag</param>
        /// <returns>Boolean</returns>
        public static bool SetAudioMute(bool mute)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.SetAudioMute(mute);
        }

        /// <summary>
        /// Toggle primary audio device volume mute flag
        /// </summary>
        /// <param name="mute">Boolean indicating the desired mute flag</param>
        /// <returns>Boolean</returns>
        public static bool ToggleAudioMute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.ToggleAudioMute();
        }

        /// <summary>
        /// Set the primary audio device volume level
        /// </summary>
        /// <param name="volume">Desired volume level (0-100)</param>
        public static void SetAudioVolume(double volume)
        {
            if (volume < 0 || volume > 100)
                throw new Exception("Volume level needs to be between 0 and 100");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            WindowsAPI.SetAudioVolume(volume);
        }

        /// <summary>
        /// Get current primary audio device volume level
        /// </summary>
        /// <returns>Double</returns>
        public static double GetAudioVolume()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.GetAudioVolume();
        }

        /// <summary>
        /// Turn on displays
        /// </summary>
        /// <returns>Double</returns>
        public static void TurnOnDisplays()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            WindowsAPI.TurnOnDisplays();
        }

        /// <summary>
        /// Turn off displays
        /// </summary>
        /// <returns>Double</returns>
        public static void TurnOffDisplays()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            WindowsAPI.TurnOffDisplays();
        }

        /// <summary>
        /// Get User Idle Time
        /// </summary>
        /// <returns>Double</returns>
        public static uint GetIdleTime()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.GetIdleTime();
        }

        /// <summary>
        /// Get Displays Information
        /// </summary>
        /// <returns></returns>
        public static List<DisplayInfo> GetDisplays()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            return WindowsAPI.GetDisplays();
        }

        /// <summary>
        /// Get Network Information
        /// </summary>
        /// <returns></returns>
        public static List<NetworkInfo> GetNetworkInfos()
        {
            List<NetworkInfo> networks = new List<NetworkInfo>();

            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface network in networkInterfaces)
                {
                    IPInterfaceProperties properties = network.GetIPProperties();
                    if (properties == null)
                        continue;

                    if (network.OperationalStatus == OperationalStatus.Up && network.Description != null && !network.Description.ToLowerInvariant().Contains("virtual") && !network.Description.ToLowerInvariant().Contains("pseudo"))
                    {
                        if (network.NetworkInterfaceType != NetworkInterfaceType.Ethernet && network.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                            continue;

                        NetworkInfo networkInfo = new NetworkInfo();
                        networkInfo.Wired = network.NetworkInterfaceType == NetworkInterfaceType.Ethernet;
                        networkInfo.Speed = network.Speed / 1000000;

                        foreach (IPAddressInformation address in properties.UnicastAddresses)
                        {
                            AddressFamily family = address.Address.AddressFamily;
                            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
                                continue;

                            if (IPAddress.IsLoopback(address.Address))
                                continue;

                            if (family == AddressFamily.InterNetwork)
                                networkInfo.IPv4Address = address.Address.ToString();
                            else if (networkInfo.IPv6Address == null)
                                networkInfo.IPv6Address = address.Address.ToString();
                        }

                        if (string.IsNullOrWhiteSpace(networkInfo.IPv4Address) && string.IsNullOrWhiteSpace(networkInfo.IPv6Address))
                            continue;

                        networks.Add(networkInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to get network information: {0}", ex.ToString());
            }

            return networks;
        }
    }
}
