using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTLinkAPI.Platform
{
    public class AudioDeviceInfo
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } 
        public double Volume { get; set; }
        public double PeakVolume { get; set; }
        public bool IsMuted { get; set; }
        public bool IsAudioPlaying { get; set; }
        public bool IsDefaultDevice { get; set; }
        public bool IsDefaultCommunicationsDevice { get; set; }
        public bool IsCaptureDevice { get; set; }
    }
}
