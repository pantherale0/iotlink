﻿using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;
using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IOTLinkAPI.Platform.Windows
{
    class AudioController
    {
        private static AudioController _instance;

        private readonly CoreAudioController audioController = new CoreAudioController();

        private CoreAudioDevice commsPlayback;
        private CoreAudioDevice mediaPlayback;

        private SortedDictionary<Guid, CoreAudioDevice> devices = new SortedDictionary<Guid, CoreAudioDevice>();
        private Dictionary<Guid, double> devicePeakValue = new Dictionary<Guid, double>();

        public static AudioController GetInstance()
        {
            if (_instance == null)
                _instance = new AudioController();

            return _instance;
        }

        private AudioController()
        {
            LoggerHelper.Trace("AudioController instance created.");

            IEnumerable<CoreAudioDevice> audioDevices = audioController.GetDevices();
            foreach (CoreAudioDevice device in audioDevices)
            {
                LoggerHelper.TraceLoop("Audio Device - ID: {0}, Name: {1}, Volume: {2}, Default: {3}, Default Comm: {4}, Capture Device: {5}, Playback Device: {6}",
                    device.Id, device.FullName, device.Volume, device.IsDefaultDevice, device.IsDefaultCommunicationsDevice, device.IsCaptureDevice, device.IsPlaybackDevice);

                OnDeviceChanged(device, DeviceChangedType.DeviceAdded);
            }

            audioController.AudioDeviceChanged.Subscribe(x => OnDeviceChanged((CoreAudioDevice)x.Device, x.ChangedType));

            if (commsPlayback == null)
                LoggerHelper.Info("No communication audio device found.");

            if (mediaPlayback == null)
                LoggerHelper.Info("No playback audio device found.");
        }

        private void OnDeviceChanged(CoreAudioDevice device, DeviceChangedType changedType)
        {
            LoggerHelper.Trace("Audio Device {0} - Change Type: {1}", device.Id, changedType);

            if (changedType == DeviceChangedType.DeviceRemoved)
            {
                devices.Remove(device.Id);
                devicePeakValue.Remove(device.Id);
            }
            else
            {
                if (device.IsPlaybackDevice)
                {
                    if (device.IsDefaultCommunicationsDevice)
                        commsPlayback = device;
                    if (device.IsDefaultDevice)
                        mediaPlayback = device;
                }

                if (changedType == DeviceChangedType.DeviceAdded)
                    device.PeakValueChanged.Subscribe(x => devicePeakValue[device.Id] = x.PeakValue);

                devices[device.Id] = device;
            }
        }

        public List<AudioDeviceInfo> GetAudioDevices()
        {
            return devices.Values.Select(x => GetAudioDeviceInfo(x.Id)).ToList();
        }

        public AudioDeviceInfo GetAudioDeviceInfo(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return null;

            return new AudioDeviceInfo
            {
                Guid = device.Id,
                Name = device.FullName,
                Volume = device.Volume,
                PeakVolume = GetAudioPeakValue(device.Id),
                IsAudioPlaying = GetAudioPeakValue(device.Id) > 0d,
                IsDefaultDevice = device.IsDefaultDevice,
                IsDefaultCommunicationsDevice = device.IsDefaultCommunicationsDevice,
                IsPlaybackDevice = device.IsPlaybackDevice,
                IsCaptureDevice = device.IsCaptureDevice
            };
        }

        public double GetAudioPeakValue(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null || !devicePeakValue.ContainsKey(device.Id))
                return 0d;

            return devicePeakValue[device.Id];
        }

        public bool SetAudioDefault(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, null);
            if (device == null)
                return false;

            return device.SetAsDefault();
        }

        public bool SetAudioDefaultComms(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, null);
            if (device == null)
                return false;

            return device.SetAsDefaultCommunications();
        }

        public bool SetAudioMute(Guid guid, bool mute)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return false;

            return device.Mute(mute);
        }

        public bool ToggleAudioMute(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return false;

            return device.ToggleMute();
        }

        public void SetAudioVolume(Guid guid, double volume)
        {
            if (volume < 0 || volume > 100)
                throw new Exception("Volume level needs to be between 0 and 100");

            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return;

            device.Volume = volume;
        }

        private CoreAudioDevice GetDeviceByGuid(Guid guid, CoreAudioDevice defaultDevice)
        {
            if (guid == Guid.Empty)
                return defaultDevice;

            if (!devices.ContainsKey(guid))
                return null;

            return devices[guid];
        }
    }
}
