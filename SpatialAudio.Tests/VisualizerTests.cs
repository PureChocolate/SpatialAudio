using SpatialAudio.Telemetry;
using SpatialAudio.Visualizer;

namespace SpatialAudio.Tests
{
    public class VisualizerTests
    {
        [Fact]
        public void AzColumn()
        {
            int width = 21;
            int[] az = { -90, 0, 90, 45, -70, -100, 200 };
            int[] val = { 0, 10, 20, 15, 2, 0, 20 };

            for (int i = 0; i < az.Length; i++)
            {
                Assert.Equal(val[i], Renderer.AzimuthColumn(az[i], width));
            }
        }

        [Fact]
        public void MapToCell()
        {
            // 200x100 virtual desktop rendered into a 40x10 cell grid.
            Assert.Equal((0, 0), Renderer.MapToCell(0, 0, 0, 0, 200, 100, 40, 10));
            Assert.Equal((39, 9), Renderer.MapToCell(199, 99, 0, 0, 200, 100, 40, 10));
            Assert.Equal((20, 5), Renderer.MapToCell(100, 50, 0, 0, 200, 100, 40, 10));
            Assert.Equal((0, 9), Renderer.MapToCell(-50, 150, 0, 0, 200, 100, 40, 10)); // clamps both axes
        }

        [Fact]
        public void FillBar()
        {
            Assert.Equal("█████░░░░░", Renderer.FillBar(0.5, 10));
            Assert.Equal("░░░░░░░░░░", Renderer.FillBar(0, 10));
            Assert.Equal("██████████", Renderer.FillBar(1, 10));
            Assert.Equal("██████████", Renderer.FillBar(1.5, 10)); // clamps above 1
        }

        [Fact]
        public void SpectrumColumns()
        {
            float[] mag = { 0, 1, 2, 3, 4, 5, 6, 7 };
            float[] dest = new float[4];

            Renderer.SpectrumColumns(mag, dest);

            // groups [0,1] [2,3] [4,5] [6,7] -> max of each
            Assert.Equal(new float[] { 1, 3, 5, 7 }, dest);
        }

        [Fact]
        public void ToDb()
        {
            float reference = 2f;

            Assert.True(MathF.Abs(Renderer.ToDb(reference, reference) - 0f) < 1e-3f);          // unity = 0 dB
            Assert.True(MathF.Abs(Renderer.ToDb(reference / 10f, reference) + 20f) < 1e-3f);   // -20 dB
            Assert.Equal(-100f, Renderer.ToDb(0f, reference));                                 // floor guard
        }

        [Fact]
        public void RenderFrame_HasFixedShape()
        {
            TelemetryFrame f = new TelemetryFrame { AzimuthDeg = 0 };

            string[] lines = Renderer.RenderFrame(f, 40);

            Assert.Equal(Renderer.frameHeight, lines.Length);
            foreach (string line in lines)
            {
                Assert.Equal(40, line.Length); // every line exactly width
            }
        }

        [Fact]
        public void RenderFrame_MarkerFollowsAzimuth()
        {
            TelemetryFrame f = new TelemetryFrame { AzimuthDeg = 0 };

            string[] lines = Renderer.RenderFrame(f, 21);

            Assert.Equal(10, lines[1].IndexOf('●'));                 // front = center column
            Assert.Equal(1, lines[1].Count(c => c == '●'));          // exactly one marker
        }
    }
}
