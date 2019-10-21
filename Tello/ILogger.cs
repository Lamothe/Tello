using System.Threading.Tasks;

namespace Tello
{
    public interface ILogger
    {
        Task WriteDebugLine(string message);

        Task WriteInformationLine(string message);

        Task WriteWarningLine(string message);

        Task WriteErrorLine(string message);
    }
}
