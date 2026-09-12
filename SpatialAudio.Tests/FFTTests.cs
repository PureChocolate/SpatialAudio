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
            for(int i = 0; i < re2.Length; i++)
            {
                Assert.True(Math.Abs(re2[i] - a[i]) < tol, $"Expected < {tol} or ~0, Actual: {re2[i] - a[i]}");
                Assert.True(Math.Abs(im2[i]) < tol, $"Expected < {tol} or ~0, Actual: {im2[i]}");
            }
        }

        [Fact]
        public void FFTIterVsRecur()
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

                float[] im = new float[t.Length];
                (float[] ffR, float[] ffI) = Spatializer.FFTProcess(t, im);
                Spatializer.FFTProcessIter(t, im);

                for (int k = 0; k < t.Length; k++)
                {
                    float diff = MathF.Abs(t[k] - ffR[k]);
                    float diff2 = MathF.Abs(im[k] - ffI[k]);

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
        public void IFFTIterReversal()
        {
            float tol = 5e-5f;
            Random seed = new Random(42);
            float[] re = new float[512];
            for (int i = 0; i < re.Length; i++) re[i] = ((seed.NextSingle() * 2.0f) - 1.0f);
            float[] original = (float[])re.Clone();
            float[] im = new float[re.Length];

            Spatializer.FFTProcessIter(re, im);
            Spatializer.IFFTProcessIter(re, im);

            for (int i = 0; i < re.Length; i++)
            {
                Assert.True(MathF.Abs(re[i] - original[i]) < tol, $"Expected < {tol} or ~0, Actual: {re[i] - original[i]}");
                Assert.True(MathF.Abs(im[i]) < tol, $"Expected < {tol} or ~0, Actual: {im[i]}");
            }
        }

        [Fact]
        public void IFFTIterVsRecur()
        {
            float tol = 5e-5f;
            Random seed = new Random(42);
            float[] a = new float[512];
            for (int i = 0; i < a.Length; i++) a[i] = ((seed.NextSingle() * 2.0f) - 1.0f);

            (float[] specRe, float[] specIm) = Spatializer.FFTProcess(a, new float[a.Length]);

            float[] iterRe = (float[])specRe.Clone();
            float[] iterIm = (float[])specIm.Clone();
            Spatializer.IFFTProcessIter(iterRe, iterIm);
            (float[] recRe, float[] recIm) = Spatializer.IFFTProcess(specRe, specIm);

            for (int i = 0; i < a.Length; i++)
            {
                Assert.True(MathF.Abs(iterRe[i] - recRe[i]) < tol, $"Real Expected < {tol}, Actual: {iterRe[i] - recRe[i]}, at i: {i}");
                Assert.True(MathF.Abs(iterIm[i] - recIm[i]) < tol, $"Imag Expected < {tol}, Actual: {iterIm[i] - recIm[i]}, at i: {i}");
            }
        }

        [Fact]
        public void Resampled()
        {
            float tol = 2 * (MathF.Pow((2 * MathF.PI * 1000) / 44100, 2) / 8); //calculate tol and double it for margin (w*dT)^2 / 8 = (2pi * 1000)^2 / 8
            float[] x = new float[512];
            for(int i = 0; i < x.Length; i++)
            {
                x[i] = MathF.Sin(2 * MathF.PI * 1000 * i / 44100);
            }
            float[] xRes = Spatializer.Resample(x, 44100.0,48000.0);
            for(int i =0; i < xRes.Length; i++)
            {
                Assert.True(Math.Abs(MathF.Sin(2 * MathF.PI * 1000 * i / 48000) - xRes[i]) < tol);
            }
        }

        [SkippableFact]
        public void ResampleKEMAR()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            float[] az45 = (float[])Spatializer._tableHReL[9].Clone();
            float[] imaz45 = (float[])Spatializer._tableHImL[9].Clone();

            Assert.Equal(1024, Spatializer._tableHReL[9].Length);
            Assert.Equal(1024, Spatializer._tableHImL[9].Length);

            Spatializer.IFFTProcessIter(az45, imaz45);
            float[] az45R = (float[])Spatializer._tableHReR[9].Clone();
            float[] imaz45R = (float[])Spatializer._tableHImR[9].Clone();
            Assert.Equal(1024, Spatializer._tableHReR[9].Length);
            Assert.Equal(1024, Spatializer._tableHImR[9].Length);
            Spatializer.IFFTProcessIter(az45R, imaz45R);

            float[] goldenL = {-1.5195722e-05f, -1.5195722e-05f, -1.5195722e-05f, -1.5195722e-05f,
            -1.5195722e-05f, -1.0684492e-05f, -7.5978612e-06f, -4.3212835e-06f};
            float[] goldenR = {0f, 1.0144337e-05f, 4.1060412e-06f, -7.677262e-06f,
             -1.8494438e-05f, -1.8804979e-05f, -9.4887508e-06f, 5.5724845e-06f};

            for(int i = 0; i < goldenL.Length; i++)
            {
                Assert.True(Math.Abs(az45[i] - goldenL[i]) < 1e-6);
                Assert.True(Math.Abs(az45R[i] - goldenR[i]) < 1e-6);
            }
        }
    }
}
