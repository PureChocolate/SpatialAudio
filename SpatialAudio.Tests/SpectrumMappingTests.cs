using SpatialAudio.Visualizer.Core;

namespace SpatialAudio.Tests
{
    public class SpectrumMappingTests
    {
        [Fact]
        public void SpectrumColumnsLog()
        {
            float[] mag = { 0, 1, 2, 3, 4, 5, 6, 7 };
            float[] dest = new float[3];
            SpectrumMapping.Columns(mag, dest, binHz: 1000f, minHz: 1000f, maxHz: 8000f);
            // per-column log bin ranges (no gaps): col0 picks bin1=1; col1 picks max(2,3)=3; col2 picks max(4..7)=7
            Assert.Equal(new float[] { 1, 3, 7 }, dest);
        }

        [Fact]
        public void ToDb()
        {
            float reference = 2f;

            Assert.True(MathF.Abs(SpectrumMapping.ToDb(reference, reference) - 0f) < 1e-3f);          // unity = 0 dB
            Assert.True(MathF.Abs(SpectrumMapping.ToDb(reference / 10f, reference) + 20f) < 1e-3f);   // -20 dB
            Assert.Equal(-100f, SpectrumMapping.ToDb(0f, reference));                                 // floor guard
        }
    }
}
