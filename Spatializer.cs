namespace SpatialAudio
{
    internal static class Spatializer
    {
        public static float CurrentAzimuthDeg { get; set; }
        private static float[] _ring = new float[32]; //basic ITD buffer 0.6ms so at 48khz ~17cm head thats roughly ~30 samples, 32 for safety.
        private static int _ringPos = 0;
        //512 - frame length rings, 1 slot per frame per ear
        private static float[] _hrtfRingL = new float[512];
        private static float[] _hrtfRingR = new float[512];
        private static int _hrtfPosL = 0;
        private static int _hrtfPosR = 0;

        public static void Process(float[] samples, int sampleRate, float azimuthDeg)
        {
            float azRad = azimuthDeg * MathF.PI / 180f;
            int delay = (int)MathF.Round(30f * MathF.Sin(MathF.Abs(azRad)));
            if (delay > _ring.Length - 1) delay = _ring.Length - 1;

            int delayedIndex = azRad < 0 ? 1 : 0;

            float pan = Math.Clamp(azRad, -MathF.PI / 2f, MathF.PI / 2f);
            float leftGain = MathF.Cos( pan / 2f + MathF.PI / 4f);
            float rightGain = MathF.Sin(pan / 2f + MathF.PI / 4f);

            // Process 960 samples, 2 per loop hence /2.
            for (int i = 0; i < samples.Length / 2; i++)
            {
                //apply gains per channel
                int delayedChannel = i * 2 + delayedIndex;
                samples[i * 2] *= leftGain;
                samples[i * 2 + 1] *= rightGain;

                //write data to ring buffer, read previous data(sound that arrives "late"/the other ear into sample from buffer, update position
                _ring[_ringPos] = samples[delayedChannel];
                samples[delayedChannel] = _ring[(_ringPos - delay + _ring.Length) % _ring.Length];
                _ringPos = (_ringPos + 1) % _ring.Length;
            }
        }

        public static float[] HRTFProcess(float[] h, float[] hR, float[] x)
        {
            float[] processed = new float[x.Length];
            //L channel
            for(int i = 0; i < processed.Length; i += 2)
            {
                for(int k = 0; k < h.Length; k++)
                {
                    if (i - (2*k) >= 0) processed[i] += h[k] * x[i - k*2]; // 2k because we are going by frames, data stream is interleved so 2 points = 1 frame
                    // we want the data from k-th frame back, i/2 gives current frame read, k-i/2 becomes -1,-2 etc and pos-1 is where the last data lives so we read perfectly % is just to wrap if we underflow.
                    else processed[i] += h[k] * _hrtfRingL[(_hrtfPosL - (k - i/2) + _hrtfRingL.Length) % _hrtfRingL.Length];
                }
            }

            //R channel
            for (int i = 1; i < processed.Length; i += 2)
            {
                for (int k = 0; k < hR.Length; k++)
                {
                    if (i - (2 * k) >= 0) processed[i] += hR[k] * x[i - 2*k];
                    else processed[i] += hR[k] * _hrtfRingR[(_hrtfPosR - (k - i/2) + _hrtfRingR.Length) % _hrtfRingR.Length];
                }
            }

            //Update rings, Push new/current data after process so we dont get current data overlap when reading back
            for (int j = 0; j < x.Length; j += 2)
            {
                _hrtfRingL[_hrtfPosL] = x[j];
                _hrtfPosL = (_hrtfPosL + 1) % _hrtfRingL.Length;
                _hrtfRingR[_hrtfPosR] = x[j + 1];
                _hrtfPosR = (_hrtfPosR + 1) % _hrtfRingR.Length;
            }

            return processed;
        }
    }
}
