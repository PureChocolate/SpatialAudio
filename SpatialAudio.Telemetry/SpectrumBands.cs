namespace SpatialAudio.Telemetry
{
    public static class SpectrumBands
    {
        public const int Count = 96;
        public const float MinHz = 100f;
        public const float MaxHz = 16000f;
        public static float LowHz(int band)
        {
            return MinHz * MathF.Pow(MaxHz / MinHz, band / (float)Count);
        }

        public static float HighHz(int band)
        {
            return MinHz * MathF.Pow(MaxHz / MinHz, (band + 1) / (float)Count);
        }
    }
}
