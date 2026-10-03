using System.IO.Pipes;

namespace SpatialAudio.Telemetry
{
    public sealed class TelemetryClient : IDisposable
    {
        readonly string _pipeName;
        NamedPipeClientStream? _pipe;

        public TelemetryClient(string pipeName = TelemetryProtocol.DefaultPipeName)
        {
            _pipeName = pipeName;
        }

        public bool Connect(int timeoutMs)
        {
            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.In);
            try
            {
                _pipe.Connect(timeoutMs);
                return true;
            }
            catch (TimeoutException) { 
                _pipe.Dispose();
                _pipe = null;
                return false;
            }
        }

        public TelemetryFrame? ReadFrame()
        {
            if (_pipe != null)
            {
                return TelemetryCodec.ReadFrame(_pipe);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Dispose()
        {
            _pipe?.Dispose();
        }
    }
}
