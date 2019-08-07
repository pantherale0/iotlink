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
            if (commsPlayback != null)
            {
                commsPlayback.VolumeChanged.Subscribe(x => commsVolume = x.Volume);
                commsPlayback.MuteChanged.Subscribe(x => commsMuted = x.IsMuted);
                commsPlayback.PeakValueChanged.Subscribe(x => commsPeakValue = x.PeakValue);
                commsVolume = commsPlayback.Volume;
                commsMuted = commsPlayback.IsMuted;
            }

            mediaPlayback = audioController.DefaultPlaybackDevice;
            if (mediaPlayback != null)
            {
                mediaPlayback.VolumeChanged.Subscribe(x => mediaVolume = x.Volume);
                mediaPlayback.MuteChanged.Subscribe(x => mediaMuted = x.IsMuted);
                mediaPlayback.PeakValueChanged.Subscribe(x => mediaPeakValue = x.PeakValue);
                mediaVolume = mediaPlayback.Volume;
                mediaMuted = mediaPlayback.IsMuted;
            }

            if (commsPlayback == null)
                LoggerHelper.Info("No communication audio device found.");

            if (mediaPlayback == null)
                LoggerHelper.Info("No playback audio device found.");
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
            if (mediaPlayback == null)
                return false;

            return mediaPlayback.Mute(mute);
        }

        public bool ToggleAudioMute()
        {
            if (mediaPlayback == null)
                return false;

            return mediaPlayback.ToggleMute();
        }

        public void SetAudioVolume(double volume)
        {
            if (mediaPlayback == null)
                return;

            if (volume < 0 || volume > 100)
                throw new Exception("Volume level needs to be between 0 and 100");

            mediaPlayback.Volume = volume;
        }
    }
}
