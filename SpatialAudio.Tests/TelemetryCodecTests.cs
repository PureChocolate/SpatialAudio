using SpatialAudio.Telemetry;

namespace SpatialAudio.Tests
{
    public class TelemetryCodecTests
    {
        [Fact]
        public void RountTrip_AllFieldsPreserved()
        {
            TelemetryFrame frame = new TelemetryFrame();
            frame.Sequence = 7;
            frame.AzimuthDeg = -42.5f;
            frame.DistancePx = 1234;
            frame.WindowTitle = "Spotify – Café";
            frame.LevelL = 0.5f;
            frame.LevelR = 0.25f;
            for(int i = 0; i < TelemetryFrame.SpectrumBins; i++)
            {
                frame.MagL[i] = i * 0.01f;
                frame.MagR[i] = 1f - (i * .01f);
            }
            MemoryStream m = new MemoryStream();
            TelemetryCodec.WriteFrame(m, frame);
            m.Position = 0;
            TelemetryFrame? f = TelemetryCodec.ReadFrame(m);
            Assert.NotNull(f);

            Assert.Equal(frame.Sequence, f.Sequence);
            Assert.True(Math.Abs(frame.AzimuthDeg - f.AzimuthDeg) < 1e-4f);
            Assert.True(Math.Abs(frame.DistancePx - f.DistancePx) < 1e-4f);
            Assert.Equal(frame.WindowTitle, f.WindowTitle);
            Assert.True(Math.Abs(frame.LevelL - f.LevelL) < 1e-4f);
            Assert.True(Math.Abs(frame.LevelR - f.LevelR) < 1e-4f);
            for(int i = 0;i < TelemetryFrame.SpectrumBins; i++)
            {
                Assert.True(Math.Abs(frame.MagL[i] - f.MagL[i]) < 1e-4f);
                Assert.True(Math.Abs(frame.MagR[i] - f.MagR[i]) < 1e-4f);
            }
        }

        [Fact]
        public void ReadFrame_BacktoBack_KeepOrder()
        {
            TelemetryFrame frame = new TelemetryFrame();
            frame.Sequence = 0;
            frame.AzimuthDeg = -42.5f;
            frame.DistancePx = 1234;
            frame.WindowTitle = "Spotify – Café";
            frame.LevelL = 0.5f;
            frame.LevelR = 0.25f;
            TelemetryFrame frame2 = new TelemetryFrame();
            frame2.Sequence = 1;
            frame2.AzimuthDeg = -42.5f;
            frame2.DistancePx = 1234;
            frame2.WindowTitle = "Spotify – Café";
            frame2.LevelL = 0.5f;
            frame2.LevelR = 0.25f;
            TelemetryFrame frame3 = new TelemetryFrame();
            frame3.Sequence = 2;
            frame3.AzimuthDeg = -42.5f;
            frame3.DistancePx = 1234;
            frame3.WindowTitle = "Spotify – Café";
            frame3.LevelL = 0.5f;
            frame3.LevelR = 0.25f;
            
            MemoryStream m = new MemoryStream();
            TelemetryCodec.WriteFrame(m, frame);
            TelemetryCodec.WriteFrame(m, frame2);
            TelemetryCodec.WriteFrame(m, frame3);
            m.Position = 0;
            TelemetryFrame? f = TelemetryCodec.ReadFrame(m);
            TelemetryFrame? f2 = TelemetryCodec.ReadFrame(m);
            TelemetryFrame? f3 = TelemetryCodec.ReadFrame(m);
            Assert.Equal(frame.Sequence, f!.Sequence);
            Assert.Equal(frame2.Sequence, f2!.Sequence);
            Assert.Equal(frame3.Sequence, f3!.Sequence);
        }

        [Fact]
        public void ReadFrame_EmptyStream()
        {
            TelemetryFrame? f = TelemetryCodec.ReadFrame(new MemoryStream());
            Assert.Null(f);
        }

        [Fact]
        public void ReadFrame_TruncatedFrame_Throws()
        {
            TelemetryFrame frame = new TelemetryFrame();
            frame.Sequence = 0;
            frame.AzimuthDeg = -42.5f;
            frame.DistancePx = 1234;
            frame.WindowTitle = "Spotify – Café";
            frame.LevelL = 0.5f;
            frame.LevelR = 0.25f;
            MemoryStream m = new MemoryStream();
            TelemetryCodec.WriteFrame(m, frame);
            m.SetLength(m.Length / 2);
            m.Position = 0;

            Assert.Throws<EndOfStreamException>(() => TelemetryCodec.ReadFrame(m));
        }
    }
}
