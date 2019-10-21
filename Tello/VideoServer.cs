using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Tello
{
    public class VideoServer : NetworkServer
    {
        private ILogger logger;

        public VideoServer(ILogger logger) :
            base(logger, 11111)
        {
            this.logger = logger;
            this.OnServerData += StateServer_OnServerData;
        }

        private void StateServer_OnServerData(byte[] data)
        {
        }
    }
}
