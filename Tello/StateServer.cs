using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Tello
{
    public class StateServer : NetworkServer
    {
        private Logger logger;

        public delegate void OnStateUpdateHandler();
        public event OnStateUpdateHandler OnStateUpdate;

        public ConcurrentDictionary<string, string> State = new ConcurrentDictionary<string, string>();

        public StateServer(Logger logger) :
            base(logger, 8890)
        {
            this.logger = logger;
            this.OnServerData += StateServer_OnServerData;
        }

        private void StateServer_OnServerData(byte[] data)
        {
            var stateString = System.Text.Encoding.ASCII.GetString(data);
            if (stateString != null)
            {
                foreach (var entry in stateString.Split(';'))
                {
                    var parts = entry.Split(':');
                    if (parts.Length == 2)
                    {
                        State[parts[0]] = parts[1];
                    }
                }
                logger.WriteDebugLine($"Got State: '{stateString}'");
                OnStateUpdate?.Invoke();
            }
        }
    }
}
