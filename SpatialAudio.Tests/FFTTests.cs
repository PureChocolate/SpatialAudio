namespace SpatialAudio.Tests
{
    public class FFTTests
    {
        [Fact]
        public void FFTCompareProbes()
        {
            float tol8 = 1e-5f;
            float tol512 = 5e-5f; // measured |FFT - Probes| for error rate on fixed inputs below, Re 3.05e-5, Im 1.62e-5 (N=512)
            float[][] a =
            {
                    new float[8], new float[8], new float[8], new float[512]
            };
            a[0][0] = 1f;
            for (int v = 0; v < a[1].Length; v++)
            {
                a[1][v] = MathF.Cos(2 * MathF.PI * 1 * v / a[1].Length);
                a[2][v] = MathF.Sin(2 * MathF.PI * 1 * v / a[1].Length);
            }
            Random seed = new Random(42);
            for (int i = 0; i < a[3].Length; i++)
            {
                a[3][i] = ((seed.NextSingle() * 2.0f) - 1.0f);
            }
            foreach (float[] t in a)
            {
                Assert.True(t.Length % 2 == 0, $"Data size not of 2^n");

                (float[] ffR, float[] ffI) = Spatializer.FFTProcess(t, new float[t.Length]);
                for (int k = 0; k < t.Length; k++)                {
                    (float f, float g) = Spatializer.Probes(t, k);
                    float diff = MathF.Abs(f - ffR[k]);
                    float diff2 = MathF.Abs(g + ffI[k]);

                    if (t.Length == 8)
                    {
                        Assert.True(diff < tol8, $"Real Expected diff: {tol8}, Actual: {diff}, at k: {k}");
                        Assert.True(diff2 < tol8, $"Imaginary Expected diff: {tol8}, Actual: {diff2}, at k: {k}");
                    }
                    else if (t.Length == 512)
                    {
                        Assert.True(diff < tol512, $"Real Expected diff: {tol512}, Actual: {diff}, at k: {k}");
                        Assert.True(diff2 < tol512, $"Imaginary Expected diff: {tol512}, Actual: {diff2}, at k: {k}");
                    }
                }
            }
        }

        [Fact]
        public void IFFTReversal()
        {
            float tol = 5e-5f;
            Random seed = new Random(42);
            float[] a = new float[512];
            for (int i = 0; i < a.Length; i++) a[i] = ((seed.NextSingle() * 2.0f) - 1.0f);

            (float[] re, float[] im) = Spatializer.FFTProcess(a, new float[a.Length]);
            (float[] re2, float[] im2) = Spatializer.IFFTProcess(re, im);
            for (int i = 0; i < re2.Length; i++)
            {
                re2[i] /= re2.Length;
                im2[i] /= im2.Length;
            }
            for(int i = 0; i < re2.Length; i++)
            {
                Assert.True(Math.Abs(re2[i] - a[i]) < tol, $"Expected < {tol} or ~0, Actual: {re2[i] - a[i]}");
                Assert.True(Math.Abs(im2[i]) < tol, $"Expected < {tol} or ~0, Actual: {im2[i]}");
            }
        }
    }
}
