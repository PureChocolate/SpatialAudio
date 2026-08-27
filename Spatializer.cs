namespace SpatialAudio
{
    internal static class Spatializer
    {
        public static float CurrentAzimuthDeg { get; set; }
        private static float[] _ring = new float[32];
        private static int _ringPos = 0;
        private static float[] _hrtfRing = new float[512];
        private static int _hrtfPos = 0;

        public static void Process(float[] samples, int sampleRate, float azimuthDeg)
        {
            float azRad = azimuthDeg * MathF.PI / 180f;
            int delay = (int)MathF.Round(30f * MathF.Sin(MathF.Abs(azRad)));
            if (delay > _ring.Length - 1) delay = _ring.Length - 1;

            int delayedIndex = azRad < 0 ? 1 : 0;

            float pan = Math.Clamp(azRad, -MathF.PI / 2f, MathF.PI / 2f);
            float leftGain = MathF.Cos( pan / 2f + MathF.PI / 4f);
            float rightGain = MathF.Sin(pan / 2f + MathF.PI / 4f);

            for (int i = 0; i < samples.Length / 2; i++)
            {
                int delayedChannel = i * 2 + delayedIndex;
                samples[i * 2] *= leftGain;
                samples[i * 2 + 1] *= rightGain;

                _ring[_ringPos] = samples[delayedChannel];
                samples[delayedChannel] = _ring[(_ringPos - delay + _ring.Length) % _ring.Length];
                _ringPos = (_ringPos + 1) % _ring.Length;
            }
        }

        public static float[] HRTFProcess(float[] h, float[] x)
        {
            float[] processed = new float[x.Length];
            for(int i = 0; i < processed.Length; i++)
            {
                for(int k = 0; k < h.Length; k++)
                {
                    if (i - k >= 0) processed[i] += h[k] * x[i - k];
                    else processed[i] += h[k] * _hrtfRing[(_hrtfPos - (k - i) + _hrtfRing.Length) % _hrtfRing.Length];
                }
            }
            for(int j = 0; j < x.Length; j++)
            {
                _hrtfRing[_hrtfPos] = x[j];
                _hrtfPos = (_hrtfPos + 1) % _hrtfRing.Length;
            }
            return processed;
        }
    }
}
