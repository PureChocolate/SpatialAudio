namespace SpatialAudio.Tests
{
    public class ConvolutionTests
    {
        // Card 5 — Y = H·X pointwise must reproduce the direct-form convolution.
        [SkippableFact]
        public void ConvolutionTheorem_MatchesDirectForm()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());

            float[] h = HrtfDatabase.GetIr(0, 45, "L");
            float[] h0 = new float[1024];
            Array.Copy(h, h0, h.Length);

            float[] x0 = new float[1024];
            x0[0] = 1f;
            x0[1] = 0.5f;

            (float[] hRe, float[] hIm) = Spatializer.FFTProcess(h0, new float[h0.Length]);
            (float[] xRe, float[] xIm) = Spatializer.FFTProcess(x0, new float[x0.Length]);

            float[] yRe = new float[1024];
            float[] yIm = new float[1024];
            for (int i = 0; i < yRe.Length; i++)
            {
                yRe[i] = hRe[i] * xRe[i] - hIm[i] * xIm[i];
                yIm[i] = hRe[i] * xIm[i] + hIm[i] * xRe[i];
            }

            (float[] y, float[] _) = Spatializer.IFFTProcess(yRe, yIm);

            AssertClose(-6.104e-05f, y[0], 1e-2f);
            AssertClose(-9.155e-05f, y[1], 1e-2f);
            AssertClose(-9.155e-05f, y[2], 1e-2f);
        }

        // Card 6 — the production streaming path must match the direct-form oracle.
        [SkippableFact]
        public void OLAProcess_MatchesDirectForm()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            Spatializer.LoadHRTF(0, 45);
            Spatializer.Reset();

            float[] stereo = new float[2880];
            for (int f = 0; f < 1440; f++)
            {
                stereo[2 * f] = 0.5f * MathF.Sin(2f * MathF.PI * 7 * f / 480f);
                stereo[2 * f + 1] = stereo[2 * f];
            }

            float[] destDirect = new float[960];
            float[] destOla = new float[960];
            float maxErr = 0;
            for (int b = 0; b < stereo.Length; b += 960)
            {
                float[] chunk = new float[960];
                Array.Copy(stereo, b, chunk, 0, 960);
                Spatializer.HRTFProcess(chunk, destDirect);
                Spatializer.OLAProcess(chunk, destOla);
                for (int i = 0; i < 960; i++)
                {
                    maxErr = MathF.Max(maxErr, MathF.Abs(destDirect[i] - destOla[i]));
                }
            }

            Assert.True(maxErr < 1e-4f, $"Expected OLA vs direct < 1e-4, Actual: {maxErr}");
        }

        // Card 7 — one realistic packet (2880 frames = 6 sub-blocks) in a single call.
        [SkippableFact]
        public void OLAProcess_LargeChunk_MatchesDirectForm()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            Spatializer.LoadHRTF(0, 45);
            Spatializer.Reset();

            float[] stereo = new float[5760];
            for (int f = 0; f < 2880; f++)
            {
                stereo[2 * f] = 0.5f * MathF.Sin(2f * MathF.PI * 7 * f / 480f);
                stereo[2 * f + 1] = stereo[2 * f];
            }

            float[] destDirect = new float[5760];
            float[] destOla = new float[5760];
            Spatializer.HRTFProcess(stereo, destDirect);
            Spatializer.OLAProcess(stereo, destOla);

            float maxErr = 0;
            for (int i = 0; i < stereo.Length; i++)
            {
                maxErr = MathF.Max(maxErr, MathF.Abs(destDirect[i] - destOla[i]));
            }

            Assert.True(maxErr < 1e-4f, $"Expected OLA vs direct < 1e-4, Actual: {maxErr}");
        }

        // Card 6 — OverlapAdd goldens against the real KEMAR IR.
        [SkippableFact]
        public void OverlapAdd_RealIrGoldens()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());

            float[] h = HrtfDatabase.GetIr(0, 45, "L");
            float[] stream = new float[1440];
            for (int i = 0; i < stream.Length; i++)
            {
                stream[i] = 0.5f * MathF.Sin(2f * MathF.PI * 7 * i / 480f);
            }

            float[] y = Spatializer.OverlapAdd(h, stream, 480);

            AssertClose(0.11588243f, y[479], 1e-3f);
            AssertClose(0.13422221f, y[1024], 1e-3f);
            AssertClose(3.351271e-05f, y[1950], 1e-3f);
        }

        private static void AssertClose(float expected, float actual, float relTol)
        {
            float tol = relTol * MathF.Abs(expected) + 1e-6f;
            Assert.True(MathF.Abs(expected - actual) <= tol, $"Expected {expected} (±{tol}), Actual: {actual}");
        }
    }
}
