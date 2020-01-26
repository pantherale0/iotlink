using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;
using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;

namespace IOTLinkAPI.Platform.Windows
{
    class AudioController
    {
        private static AudioController _instance;

        private readonly CoreAudioController audioController = new CoreAudioController();

        private CoreAudioDevice commsPlayback;
        private CoreAudioDevice mediaPlayback;

        private Dictionary<Guid, CoreAudioDevice> devices = new Dictionary<Guid, CoreAudioDevice>();
        private Dictionary<Guid, double> deviceVolume = new Dictionary<Guid, double>();
        private Dictionary<Guid, double> devicePeakValue = new Dictionary<Guid, double>();
        private Dictionary<Guid, bool> deviceMuted = new Dictionary<Guid, bool>();
        private Dictionary<Guid, string> deviceState = new Dictionary<Guid, string>();

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
                LoggerHelper.TraceLoop("Audio Device - ID: {}, Real ID: {}, Name: {}", device.Id, device.RealId, device.FullName);
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
            LoggerHelper.Trace("Audio Device {} - Change Type: {}", device.Id, changedType);

            if (changedType == DeviceChangedType.DeviceRemoved)
            {
                devices.Remove(device.Id);
                deviceVolume.Remove(device.Id);
                devicePeakValue.Remove(device.Id);
                deviceMuted.Remove(device.Id);
                deviceState.Remove(device.Id);
            }
            else
            {
                if (device.IsDefaultCommunicationsDevice)
                    commsPlayback = device;
                if (device.IsDefaultDevice)
                    mediaPlayback = device;

                if (changedType == DeviceChangedType.DeviceAdded)
                    device.PeakValueChanged.Subscribe(x => devicePeakValue[device.Id] = x.PeakValue);

                devices[device.Id] = device;
                deviceMuted[device.Id] = device.IsMuted;
                deviceState[device.Id] = device.State.ToString();
                deviceVolume[device.Id] = device.Volume;
            }
        }

        public bool IsAudioMuted(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return false;

            return deviceMuted[device.Id];
        }

        public bool IsAudioPlaying(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return false;

            return devicePeakValue[device.Id] > 0d;
        }

        public double GetAudioVolume(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return 0d;

            return deviceVolume[device.Id];
        }

        public double GetAudioPeakValue(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return 0d;

            return devicePeakValue[device.Id];
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

        public void SetDefaultDevice(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return;

            device.SetAsDefault();
        }

        public void SetDefaultCommunicationDevice(Guid guid)
        {
            CoreAudioDevice device = GetDeviceByGuid(guid, mediaPlayback);
            if (device == null)
                return;

            device.SetAsDefaultCommunications();
        }

        private CoreAudioDevice GetDeviceByGuid(Guid guid, CoreAudioDevice defaultDevice)
        {
            if (guid == Guid.Empty || !devices.ContainsKey(guid))
                return defaultDevice;

            return devices[guid];
        }
    }
}
