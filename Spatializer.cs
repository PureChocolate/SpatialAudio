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
                sum += x[n] * (float)Math.Cos(2 * Math.PI * k * ((double)n / x.Length));
                sumS += x[n] * (float)Math.Sin(2 * Math.PI * k * ((double)n / x.Length));
            }

            return (sum,sumS);
        }

        // Radix-2 FFT (decimation in time): even/odd parity split per level, recursion
        // to N=1, butterfly X[k] = E + w^k·O, X[k+N/2] = E − w^k·O. N = re.Length.
        // Verified vs the direct DFT (Probes): N=8 tables (impulse/cos1/sin1) and
        // N=512 random max-diff ~2e-5 (mode 3 harness). Note: the FFT is MORE accurate
        // than the direct reference — Probes needs double-precision angles because its
        // k·n arguments run far beyond float32's precision (k up to 511, n up to 511).
        public static (float[], float[]) FFTProcess(float[] re, float[] im)
        {
            //Seperate into even odds arrays, so we "alternate" the data that gets shifted every pass.
            if (re.Length == 1) return (re, im);
            float[] eRe = new float[re.Length / 2];
            float[] eIm = new float[im.Length / 2];
            float[] oRe = new float[re.Length / 2];
            float[] oIm = new float[im.Length / 2];
            for (int i = 0; i < re.Length / 2; i++)
            {
                eRe[i] = re[2 * i];
                eIm[i] = im[2 * i];
                oRe[i] = re[(2 * i) + 1];
                oIm[i] = im[(2 * i) + 1];
            }

            //process out arrays to generate real, imaginary arrays on even and odd
            (float[] eR, float[] Ei) = FFTProcess(eRe, eIm);
            (float[] oR, float[] oI) = FFTProcess(oRe, oIm);

            float[] outRe = new float[re.Length];
            float[] outIm = new float[re.Length];

            //combine and do the final calculation
            for(int k = 0; k < re.Length/2; k++)
            {
                //angle calculation/the shift for the point
                float wR = MathF.Cos(-2f * MathF.PI * k / re.Length);
                float wI = MathF.Sin(-2f * MathF.PI * k / re.Length);
                //shift the points 
                float tR = wR * oR[k] - wI * oI[k];
                float tI = wR * oI[k] + wI * oR[k];
                //combine the even and odd parts for the first half, evens are the baseline to measure the shift of odd's
                outRe[k] = eR[k] + tR;
                outIm[k] = Ei[k] + tI;
                //fill out the 2nd half of the data since its 180 degree flip
                //cos(x + 180) = -cos(x), sin(x+180) = -sin(x), and tR is x coord tI is y cord, (cos,sin)
                outRe[k + re.Length/2] = eR[k] - tR;
                outIm[k + re.Length / 2] = Ei[k] - tI;
            }
            return (outRe,outIm);
        }
    }
}
