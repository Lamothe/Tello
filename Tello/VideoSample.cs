using System;
using System.Collections.Generic;
using System.Text;

namespace Tello
{
    public class VideoSample
    {
        public byte[] Buffer { get; }
        public TimeSpan TimeIndex { get; }
        public TimeSpan Duration { get; }
        public int Length => Buffer.Length;

        public VideoSample(byte[] buffer, TimeSpan timeIndex, TimeSpan duration)
        {
            Buffer = buffer;
            TimeIndex = timeIndex;
            Duration = duration;
        }
    }
}
