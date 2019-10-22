using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tello
{
    public class VideoServer : NetworkServer
    {
        private Logger logger;
        private readonly ConcurrentQueue<VideoSample> samples = new ConcurrentQueue<VideoSample>();
        private Stopwatch timer = new Stopwatch();
        private TimeSpan elapsed = TimeSpan.FromSeconds(0);

        public VideoServer(Logger logger) :
            base(logger, 11111)
        {
            this.logger = logger;
            this.OnServerData += VideoServer_OnServerData;
        }

        private void VideoServer_OnServerData(byte[] data)
        {
            var sample = new VideoSample(data, elapsed, timer.Elapsed - elapsed);
            elapsed = timer.Elapsed;

            if (!timer.IsRunning)
            {
                timer.Start();
            }

            while (samples.Count > 1000)
            {
                samples.TryDequeue(out VideoSample _);
            }

            samples.Enqueue(sample);
        }

        public VideoSample GetSample()
        {
            VideoSample sample;

            var wait = new SpinWait();
            while (samples.Count == 0 || !samples.TryDequeue(out sample))
            {
                wait.SpinOnce();
            }

            return sample;
        }
    }
}
