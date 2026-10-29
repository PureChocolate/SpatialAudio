namespace SpatialAudio.Visualizer.Core
{
    public static class SpectrumMapping
    {
        public static float ToDb(float value, float reference)
        {
            if (value <= 0) return -100f;
            return 20 * MathF.Log10(value / reference);
        }

        public static void Columns(float[] mag, float[] dest, float binHz, float minHz, float maxHz)
        {
            float logRange = MathF.Log(maxHz / minHz);
            int bins = mag.Length;
            for (int c = 0; c < dest.Length; c++)
            {
                float f0 = minHz * MathF.Exp(logRange * c / dest.Length);
                float f1 = minHz * MathF.Exp(logRange * (c + 1) / dest.Length);
                int b0 = Math.Clamp((int)(f0 / binHz), 0, bins - 1);
                int b1 = Math.Clamp((int)MathF.Ceiling(f1 / binHz), b0 + 1, bins);
                float max = mag[b0];
                for (int k = b0 + 1; k < b1; k++) if (mag[k] > max) max = mag[k];
                dest[c] = max;
            }
        }
    }
}
