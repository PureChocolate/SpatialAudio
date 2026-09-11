namespace SpatialAudio.Tests
{
    public class OverlapAddTests
    {
        [Fact]
        public void OverlapAdd_MatchesDirectConvolution()
        {
            float[] h = {1f, 0.5f, -0.25f};
            float[] x = {1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f};
            int blockSize = 4;

            float[] actual = Spatializer.OverlapAdd(h, x, blockSize);

            Assert.Equal(x.Length + h.Length - 1, actual.Length);
            float tol = 1e-4f;
            float[] directConv = DirectConvolve(h, x);
            for (int i = 0; i < directConv.Length; i++)
            {
                float diff = MathF.Abs(directConv[i] - actual[i]);
                Assert.True(diff < tol, $"Expected diff: < {tol}, Actual: {diff}, at index: {i}");
            }
        }

        [Fact]
        public void OverlapAdd_ThrowsWhenBlockSizeDoesNotDivideInput()
        {
            float[] h = { 1f, 0.5f, -0.25f };
            float[] x = { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f };
            
            Assert.Throws<ArgumentException>(() => Spatializer.OverlapAdd(h, x, 3));
        }

        private static float[] DirectConvolve(float[] h, float[] x)
        {

            float[] yRef = new float[x.Length + h.Length - 1];
            for (int n = 0; n < yRef.Length; n++)
            {
                for (int k = 0; k < h.Length; k++)
                {
                    if (n - k >= 0 && n - k < x.Length) yRef[n] += h[k] * x[n - k];
                }
            }
            return yRef;
        }
    }
}