using System.Threading.Tasks;

namespace Tello
{
    public abstract class Logger
    {
        public virtual async void WriteDebugLine(string message)
        {
            await Task.Yield();
        }

        public virtual async void WriteInformationLine(string message)
        {
            await Task.Yield();
        }

        public virtual async void WriteWarningLine(string message)
        {
            await Task.Yield();
        }

        public virtual async void WriteErrorLine(string message)
        {
            await Task.Yield();
        }
    }
}
