[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SpatialAudio.Tests
{
    public class HRTFTest
    {
        public class FloatPrecisonComparer : IEqualityComparer<float>
        {
            private readonly int _precision;
            public FloatPrecisonComparer(int precision) => _precision = precision;

            public bool Equals(float x, float y) => Math.Round(x, _precision) == Math.Round(y, _precision);
            public int GetHashCode(float obj) => Math.Round(obj, _precision).GetHashCode();
        }
        [SkippableFact]
        public void Reset_RestoresStreamState()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            Spatializer.LoadHRTF(0, 45);

            float[] impulse = new float[512];
            impulse[0] = 1f;
            Spatializer.Reset();
            float[] y1 = new float[512];
            Spatializer.HRTFProcess(impulse, y1);
            Spatializer.Reset();
            float[] y2 = new float[512];
            Spatializer.HRTFProcess(impulse, y2);

            Assert.Equal(y1, y2, new FloatPrecisonComparer(5));
        }

        [SkippableFact]
        public void Loader_MatchesDocumentedRegression()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            Assert.Equal(-0.8176575f, HrtfDatabase.GetIr(40, 289, "L").MaxBy(a => Math.Abs(a)), precision: 6);
            Assert.Equal(512, HrtfDatabase.GetIr(0, 45, "L").Length);
            Assert.Equal(3.8569f, HrtfDatabase.GetIr(0, 45, "L").Sum(a => MathF.Abs(a)), precision: 3);
            Assert.Equal(10.4382f, HrtfDatabase.GetIr(0,45,"R").Sum(a => MathF.Abs(a)), precision: 3);
        }

        [SkippableFact]
        public void HRTFProcess_MatchesGoldens()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            Spatializer.LoadHRTF(0, 45);
            Spatializer.Reset();

            // C — one stereo frame; far-ear R reads hR[0] = 0
            float[] c = new float[512];
            c[0] = 1f;
            c[1] = 1f;
            float[] yC = new float[512];
            Spatializer.HRTFProcess(c, yC);
            float[] cExpect = { -3.2e-5f, 0f, -3.2e-5f, 2.3e-5f, -3.2e-5f, 5.8e-6f, -3.2e-5f, -2.3e-5f };
            for (int i = 0; i < cExpect.Length; i++) AssertClose(cExpect[i], yC[i]);

            // A — impulse in, h out (definition of impulse response)
            float[] impulse = new float[512];
            impulse[0] = 1f;
            float[] yA = new float[512];
            Spatializer.HRTFProcess(impulse, yA);
            float[] aExpect = { 2.3e-3f, 4.7e-4f, 3.8e-3f, 5.2e-4f, 4.2e-3f, 9.1e-4f, 4.6e-3f, 1.1e-3f };
            for (int i = 0; i < aExpect.Length; i++) AssertClose(aExpect[i], yA[i]);

            // B — impulse + half-strength echo
            float[] half = new float[512];
            half[0] = 1f;
            half[1] = 0.5f;
            float[] yB = new float[512];
            Spatializer.HRTFProcess(half, yB);
            float[] bExpect = { 2.3e-3f, 0f, 3.8e-3f };
            for (int i = 0; i < bExpect.Length; i++) AssertClose(bExpect[i], yB[i]);

            // A2 — full chunk of ones; ring still holds B
            float[] ones = new float[512];
            Array.Fill(ones, 1f);
            float[] yA2 = new float[512];
            Spatializer.HRTFProcess(ones, yA2);
            float[] a2Expect = { 2.3e-3f, 2.3e-4f, 3.7e-3f, 2.8e-4f, 4.1e-3f, 4.8e-4f, 4.5e-3f, 5.4e-4f };
            for (int i = 0; i < a2Expect.Length; i++) AssertClose(a2Expect[i], yA2[i]);

            // B2 — zeros; ring holds A2's ones
            float[] zeros = new float[512];
            float[] yB2 = new float[512];
            Spatializer.HRTFProcess(zeros, yB2);
            float[] b2Expect = { -3.4e-3f, 8.8e-3f, 4.0e-4f };
            for (int i = 0; i < b2Expect.Length; i++) AssertClose(b2Expect[i], yB2[i]);
        }

        // Goldens are E1 (2 sig figs) so use a relative tolerance; floor pins the zeros.
        private static void AssertClose(float expected, float actual, float relTol = 0.05f)
        {
            float tol = relTol * MathF.Abs(expected) + 1e-6f;
            Assert.True(MathF.Abs(expected - actual) <= tol, $"Expected {expected} (±{tol}), Actual: {actual}");
        }
    }
}
