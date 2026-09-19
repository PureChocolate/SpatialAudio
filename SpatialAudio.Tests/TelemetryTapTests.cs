using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialAudio.Tests
{
    public class TelemetryTapTests
    {
        [Fact]
        public void PeakLevel()
        {
            float[] data = { 0.5f, -0.25f, 0.5f, -0.25f };
            float peakL = Spatializer.PeakLevel(data, 0);
            float peakR = Spatializer.PeakLevel(data, 1);

            Assert.Equal(0.5f, peakL);
            Assert.Equal(0.25f, peakR);


            float[] data2 = { -0.9f, 0.1f };
            float peakS = Spatializer.PeakLevel(data2, 0);
            Assert.Equal(0.9f, peakS);

            float peakE = Spatializer.PeakLevel([], 0);
            Assert.Equal(0, peakE);
        }

        [SkippableFact]
        public void LevelFill()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            float[] steam = new float[960];
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i] = 0.1f;
            }
            Spatializer.Process(steam, 48000, 0);
            Assert.True(Spatializer.OutputLevelL > 0 && Spatializer.OutputLevelR > 0);

            Spatializer.Reset();
            Spatializer.Process(new float[960], 48000, 0);
            Assert.Equal(0, Spatializer.OutputLevelL);
            Assert.Equal(0, Spatializer.OutputLevelR);
            Spatializer.Reset();
        }

        [SkippableFact]
        public void CheckMagnitude_MatchesIndependentFFT()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            Spatializer.LoadHRTF(0, 0);

            float[] magL = new float[512];
            float[] magR = new float[512];
            Spatializer.GetHRTFMagnitude(magL, magR);

            AssertMagnitudeMatches(magL, HrtfDatabase.GetIr(0, 0, "L"));
            AssertMagnitudeMatches(magR, HrtfDatabase.GetIr(0, 0, "R"));
        }

        // Oracle: replicate LoadHRTF's normalization + FFT independently of the tap.
        private static void AssertMagnitudeMatches(float[] actual, float[] rawIr)
        {
            float sum = 0f;
            for (int i = 0; i < rawIr.Length; i++) sum += MathF.Abs(rawIr[i]);

            float[] re = new float[1024];
            float[] im = new float[1024];
            for (int i = 0; i < rawIr.Length; i++) re[i] = rawIr[i] / sum;

            (re, im) = Spatializer.FFTProcess(re, im);

            for (int k = 0; k < actual.Length; k++)
            {
                float expected = MathF.Sqrt(re[k] * re[k] + im[k] * im[k]);
                Assert.True(MathF.Abs(actual[k] - expected) < 1e-3f,
                    $"bin {k}: expected {expected}, got {actual[k]}");
            }
        }
    }
}
