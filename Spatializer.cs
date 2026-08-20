namespace SpatialAudio
{
    internal static class Spatializer
    {
        public static float CurrentAzimuthDeg;
        public static float[] ring = new float[32];
        public static int ringPos = 0;

        public static void Process(float[] samples, int sampleRate, float azimuthDeg)
        {
            float azRad = azimuthDeg * MathF.PI / 180f;
            int delay = (int)MathF.Round(30f * MathF.Sin(MathF.Abs(azRad)));
            if (delay > ring.Length - 1) delay = ring.Length - 1;

            int delayedIndex = azRad < 0 ? 1 : 0;

            float pan = Math.Clamp(azRad, -MathF.PI / 2f, MathF.PI / 2f);
            float leftGain = MathF.Cos( pan / 2f + MathF.PI / 4f);
            float rightGain = MathF.Sin(pan / 2f + MathF.PI / 4f);

            for (int i = 0; i < samples.Length / 2; i++)
            {
                int delayedChannel = i * 2 + delayedIndex;
                samples[i * 2] *= leftGain;
                samples[i * 2 + 1] *= rightGain;

                ring[ringPos] = samples[delayedChannel];
                samples[delayedChannel] = ring[(ringPos - delay + ring.Length) % ring.Length];
                ringPos = (ringPos + 1) % ring.Length;
            }
        }
    }
}
