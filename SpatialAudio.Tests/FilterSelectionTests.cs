namespace SpatialAudio.Tests
{
    public class FilterSelectionTests
    {
        // UpdateFilter should produce table[lo]*(1-w) + table[hi]*w for all four spectra.
        private static void AssertBlend(float az, int lo, int hi, float w)
        {
            Spatializer.UpdateFilter(az);
            for (int k = 0; k < 1024; k++)
            {
                AssertClose(Spatializer._tableHReL[lo][k], Spatializer._tableHReL[hi][k], w, Spatializer._HtReL[k], "ReL", k);
                AssertClose(Spatializer._tableHImL[lo][k], Spatializer._tableHImL[hi][k], w, Spatializer._HtImL[k], "ImL", k);
                AssertClose(Spatializer._tableHReR[lo][k], Spatializer._tableHReR[hi][k], w, Spatializer._HtReR[k], "ReR", k);
                AssertClose(Spatializer._tableHImR[lo][k], Spatializer._tableHImR[hi][k], w, Spatializer._HtImR[k], "ImR", k);
            }
        }

        private static void AssertClose(float lo, float hi, float w, float actual, string label, int k)
        {
            float expected = lo * (1 - w) + hi * w;
            Assert.True(MathF.Abs(expected - actual) < 1e-6f, $"{label} k={k}: expected {expected}, actual {actual}");
        }

        [SkippableFact]
        public void UpdateFilter_SnapsToDirection()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            AssertBlend(45f, 9, 10, 0f);     // az 45   -> KEMAR 45  -> table[9]
            AssertBlend(-45f, 63, 64, 0f);   // az -45  -> KEMAR 315 -> table[63]
        }

        [SkippableFact]
        public void UpdateFilter_CrossfadesBetweenDirections()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            AssertBlend(2.5f, 0, 1, 0.5f);   // halfway between az0 and az5
        }

        [SkippableFact]
        public void UpdateFilter_WrapsAtFrontSeam()
        {
            Skip.IfNot(HrtfDatabase.IsAvailable());
            AssertBlend(-2.5f, 71, 0, 0.5f); // KEMAR 357.5: halfway between az355 and az0
        }
    }
}
