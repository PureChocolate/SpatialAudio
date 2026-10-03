using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialAudio.Telemetry
{
    public sealed class TelemetryServer : IDisposable
    {
        string _pipeName;
        Func<TelemetryFrame> _sample;
        int _intervalMs;
        volatile bool _running;
        Thread? _thread;
        NamedPipeServerStream? _pipe;

        public TelemetryServer(string pipeName, Func<TelemetryFrame> sample, int intervalMs = 33)
        {
            _pipeName = pipeName;
            _sample = sample;
            _intervalMs = intervalMs;
        }
        public void Start()
        {
            if (_running) return;
            _running = true;

            _thread = new Thread(Run) { IsBackground = true };
            _thread.Start();
        }

        private void Run()
        {
            while (_running)
            {
                using var pipe = new NamedPipeServerStream(_pipeName, PipeDirection.Out, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte);
                _pipe = pipe;

                try
                {
                    pipe.WaitForConnection();
                } catch( ObjectDisposedException) { break; }

                if (!_running) break;

                Publish(pipe);
            }
        }

        private void Publish(NamedPipeServerStream pipe)
        {
            while (_running)
            {
                TelemetryFrame frame = _sample();
                try
                {
                    TelemetryCodec.WriteFrame(pipe, frame);
                } catch(IOException) { break; }
                catch( ObjectDisposedException) { break; }
                Thread.Sleep(_intervalMs);
            }
        }
        
        public void Dispose()
        { 
            _running = false;
            _pipe?.Dispose();
            _thread?.Join(500);
        }
    }
}
