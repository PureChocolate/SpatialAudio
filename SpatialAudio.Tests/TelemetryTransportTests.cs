using SpatialAudio.Telemetry;

namespace SpatialAudio.Tests
{
    public class TelemetryTransportTests
    {
        [Fact]
        public void Server_StreamsFramesToClient()
        {
            string pipe = "SpatialAudio.Telemetry.Test." + Guid.NewGuid().ToString("N");
            long seq = 0;
            using var server = new TelemetryServer(pipe, () => new TelemetryFrame {Sequence = seq++, AzimuthDeg = -12.5f, LevelL = 0.5f}, intervalMs: 5);
            server.Start();

            using var client = new TelemetryClient(pipe);
            Assert.True(client.Connect(5000));

            for (int i = 0; i < 5; i++) { 
                var frame = client.ReadFrame();
                Assert.NotNull(frame);
                Assert.Equal(i, frame!.Sequence);
                Assert.True(MathF.Abs(frame.AzimuthDeg - -12.5f) < 1e-6f);
            }
        }
    }
}
