using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
        private static float[] _hL = new float[512];
        private static float[] _hR = new float[512];
        private static float[] _scratch = new float[960];
        private static byte[] _processed = new byte[_scratch.Length * 4];

        public static byte[] Process(float[] samples, int sampleRate, float azimuthDeg)
        {
            if(samples.Length != _scratch.Length)
            {
                _scratch = new float[samples.Length];
                _processed = new byte[_scratch.Length * 4];
            }
            HRTFProcess(samples, _scratch);
            Buffer.BlockCopy(_scratch, 0, _processed,0,_scratch.Length*4);
            return _processed;
        }

        //dest is constructed to match size of x in previous method before call
        public static void HRTFProcess(float[] x, float[] dest)
        {
            Array.Clear(dest, 0, dest.Length);
            //L channel
            for(int i = 0; i < dest.Length; i += 2)
            {
                for(int k = 0; k < _hL.Length; k++)
                {
                    if (i - (2*k) >= 0) dest[i] += _hL[k] * x[i - k*2]; // 2k because we are going by frames, data stream is interleved so 2 points = 1 frame
                    // we want the data from k-th frame back, i/2 gives current frame read, k-i/2 becomes -1,-2 etc and pos-1 is where the last data lives so we read perfectly % is just to wrap if we underflow.
                    else dest[i] += _hL[k] * _hrtfRingL[(_hrtfPosL - (k - i/2) + _hrtfRingL.Length) % _hrtfRingL.Length];
                }
                dest[i] *= 2;
            }

            //R channel
            for (int i = 1; i < dest.Length; i += 2)
            {
                for (int k = 0; k < _hR.Length; k++)
                {
                    if (i - (2 * k) >= 0) dest[i] += _hR[k] * x[i - 2*k];
                    else dest[i] += _hR[k] * _hrtfRingR[(_hrtfPosR - (k - i/2) + _hrtfRingR.Length) % _hrtfRingR.Length];
                }
                dest[i] *= 2;
            }

            //Update rings, Push new/current data after process so we dont get current data overlap when reading back
            for (int j = 0; j < x.Length; j += 2)
            {
                _hrtfRingL[_hrtfPosL] = x[j];
                _hrtfPosL = (_hrtfPosL + 1) % _hrtfRingL.Length;
                _hrtfRingR[_hrtfPosR] = x[j + 1];
                _hrtfPosR = (_hrtfPosR + 1) % _hrtfRingR.Length;
            }
        }

        public static void LoadHRTF(int ele, int az)
        {
            _hL = HrtfDatabase.GetIr(ele,az,"L");
            _hR = HrtfDatabase.GetIr(ele, az, "R");

            float sL = _hL.Sum(x => MathF.Abs(x));
            float sR = _hR.Sum(x => MathF.Abs(x));

            _hL = sL == 0 ? _hL : _hL.Select(x => x / sL).ToArray();
            _hR = sR == 0 ? _hR : _hR.Select(x => x / sR).ToArray();
        }

        public static (float,float) Probes(float[] x, int k)
        {
            float sum = 0;
            float sumS = 0;
            for (int n = 0; n < x.Length; n++) {
                sum += x[n] * MathF.Cos(2 * MathF.PI * k * ((float)n / x.Length));
                sumS += x[n] * MathF.Sin(2 * MathF.PI * k * ((float)n / x.Length));
            }

            return (sum,sumS);
        }

        // Piece 3 WIP (disabled 2026-08-25 — resume point: N=2 by hand, then N=4/8/512
        // against the direct DFT in Probes). The recursion must pass the sub-(re/ im)
        // arrays into itself, and the split/butterfly combination loop is still
        // unwritten. See LEARNING.md M3 section for the full resume path.
        //public static (float[], float[]) FFTProcess(float[] x, float[] re, float[] im)
        //{
        //    if (x.Length == 1) return (re, im);
        //    float[] eRe = new float[re.Length / 2];
        //    float[] eIm = new float[im.Length / 2];
        //    float[] oRe = new float[re.Length / 2];
        //    float[] oIm = new float[im.Length / 2];
        //    for (int i = 0; i < re.Length / 2; i++)
        //    {
        //        eRe[i] = re[2 * i];
        //        eIm[i] = im[2 * i];
        //        oRe[i] = re[(2 * i) + 1];
        //        oIm[i] = im[(2 * i) + 1];
        //    }
        //    float[] fX = x[0..(x.Length / 2)];
        //    float[] sX = x[(x.Length / 2)..];
        //    (float[] eR, float[] Ei) = FFTProcess(x, eRe, eIm);
        //    (float[] oR, float[] oI) = FFTProcess(x, oRe, oIm);

        //    //for(int n = 0; n < x.Length / 2 - 1; n++)
        //    //{
        //    //    float w = MathF.Pow(MathF.E, -2 * MathF.PI * k * (n / (float)(x.Length / 2)));
        //    //    eK += x[2 * n] * w;
        //    //    oK += x[(2 * n)+ 1] * w;
        //    //}

        //    //return (eK, oK);
        //    return (eRe, eIm);
        //}
    }
}
