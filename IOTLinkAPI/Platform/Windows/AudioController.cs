using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;
using IOTLinkAPI.Helpers;
using System;

namespace IOTLinkAPI.Platform.Windows
{
    class AudioController
    {
        private static AudioController _instance;

        private readonly CoreAudioController audioController = new CoreAudioController();

        private CoreAudioDevice commsPlayback;
        private CoreAudioDevice mediaPlayback;

        private double commsVolume;
        private double commsPeakValue;
        private bool commsMuted;

        private double mediaVolume;
        private double mediaPeakValue;
        private bool mediaMuted;

        public static AudioController GetInstance()
        {
            if (_instance == null)
                _instance = new AudioController();

            return _instance;
        }

        private AudioController()
        {
            LoggerHelper.Trace("AudioController instance created.");

            commsPlayback = audioController.DefaultPlaybackCommunicationsDevice;
            commsPlayback.VolumeChanged.Subscribe(x => commsVolume = x.Volume);
            commsPlayback.MuteChanged.Subscribe(x => commsMuted = x.IsMuted);
            commsPlayback.PeakValueChanged.Subscribe(x => commsPeakValue = x.PeakValue);
            commsVolume = commsPlayback.Volume;
            commsMuted = commsPlayback.IsMuted;

            mediaPlayback = audioController.DefaultPlaybackDevice;
            mediaPlayback.VolumeChanged.Subscribe(x => mediaVolume = x.Volume);
            mediaPlayback.MuteChanged.Subscribe(x => mediaMuted = x.IsMuted);
            mediaPlayback.PeakValueChanged.Subscribe(x => mediaPeakValue = x.PeakValue);
            mediaVolume = mediaPlayback.Volume;
            mediaMuted = mediaPlayback.IsMuted;
        }

        public bool IsAudioMuted()
        {
            return mediaMuted;
        }

        public bool IsAudioPlaying()
        {
            return mediaPeakValue > 0d;
        }

        public double GetAudioVolume()
        {
            return mediaVolume;
        }

        public double GetAudioPeakValue()
        {
            return mediaPeakValue;
        }

        public bool SetAudioMute(bool mute)
        {
            return mediaPlayback.Mute(mute);
        }

        public bool ToggleAudioMute()
        {
            return mediaPlayback.ToggleMute();
        }

        public void SetAudioVolume(double volume)
        {
            if (volume < 0 || volume > 100)
                throw new Exception("Volume level needs to be between 0 and 100");

            mediaPlayback.Volume = volume;
        }
    }
}
