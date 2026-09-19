using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SpatialAudio
{
    internal static class Spatializer
    {
        #region Fields
        public static float CurrentAzimuthDeg { get; set; }
        //512 - frame length rings, 1 slot per frame per ear
        private static float[] _hrtfRingL = new float[512];
        private static float[] _hrtfRingR = new float[512];
        private static int _hrtfPosL = 0;
        private static int _hrtfPosR = 0;
        private static float[] _hL = new float[512];
        private static float[] _hR = new float[512];
        private static float[] _scratch = new float[960];
        private static byte[] _processed = new byte[_scratch.Length * 4];

        //Buffer for each ear
        private static float[] _accL = new float[1024];
        private static float[] _accR = new float[1024];
        //padded input block
        private static float[] _block = new float[1024];
        private static float[] _blockIm = new float[1024];
        //FFT per ear
        private static float[] _ffReL = new float[1024];
        private static float[] _ffImL = new float[1024];
        private static float[] _ffReR = new float[1024];
        private static float[] _ffImR = new float[1024];
        //HRTF data fft processed per ear
        private static float[] _HReL = new float[1024];
        private static float[] _HImL = new float[1024];
        private static float[] _HReR = new float[1024];
        private static float[] _HImR = new float[1024];
        private static readonly float[] _twRe = new float[512];
        private static readonly float[] _twIm = new float[512];
        internal static float[][] _tableHReL = new float[72][];
        internal static float[][] _tableHImL = new float[72][];
        internal static float[][] _tableHReR = new float[72][];
        internal static float[][] _tableHImR = new float[72][];
        private static float _lastAZ = float.NaN;
        internal static float[] _HtReL = new float[1024];
        internal static float[] _HtImL = new float[1024];
        internal static float[] _HtReR = new float[1024];
        internal static float[] _HtImR = new float[1024];
        private static bool _filterPrimed = false;
        private const float MasterGain = 4f;

        public static float OutputLevelL { get; private set; }
        public static float OutputLevelR { get; private set; }
        #endregion

        internal static void UpdateFilter(float azimuthDeg)
        {
            float keemarAZ = azimuthDeg >= 0 ? azimuthDeg : 360f + azimuthDeg;
            float pos = keemarAZ / 5f;
            int low = (int)MathF.Floor(pos) % 72;
            int high = (low + 1) % 72;
            float w = pos - MathF.Floor(pos);

            for (int k = 0; k < 1024; k++)
            {
                _HtReL[k] = _tableHReL[low][k] * (1 - w) + _tableHReL[high][k] * w;
                _HtReR[k] = _tableHReR[low][k] * (1 - w) + _tableHReR[high][k] * w;
                _HtImR[k] = _tableHImR[low][k] * (1 - w) + _tableHImR[high][k] * w;
                _HtImL[k] = _tableHImL[low][k] * (1 - w) + _tableHImL[high][k] * w;
            }
        }

        public static byte[] Process(float[] samples, int sampleRate, float azimuthDeg)
        {
            if(azimuthDeg != _lastAZ)
            {
                _lastAZ = azimuthDeg;
                UpdateFilter(azimuthDeg);
                if (!_filterPrimed)
                {
                    Array.Copy(_HtReL, _HReL, 1024);
                    Array.Copy(_HtImL, _HImL, 1024);
                    Array.Copy(_HtReR, _HReR, 1024);
                    Array.Copy(_HtImR, _HImR, 1024);
                    _filterPrimed = true;
                }
            }
            if(samples.Length != _scratch.Length)
            {
                _scratch = new float[samples.Length];
                _processed = new byte[_scratch.Length * 4];
            }
            //HRTFProcess(samples, _scratch);
            OLAProcess(samples, _scratch);
            for (int i = 0; i < _scratch.Length; i++) _scratch[i] *= MasterGain;
            OutputLevelL = PeakLevel(_scratch, 0);
            OutputLevelR = PeakLevel(_scratch, 1);
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
            _hL = HrtfDatabase.GetIr(ele, az, "L");
            _hR = HrtfDatabase.GetIr(ele, az, "R");

            float sL = _hL.Sum(x => MathF.Abs(x));
            float sR = _hR.Sum(x => MathF.Abs(x));

            _hL = sL == 0 ? _hL : _hL.Select(x => x / sL).ToArray();
            _hR = sR == 0 ? _hR : _hR.Select(x => x / sR).ToArray();

            float[] hPL = new float[1024];
            float[] hPR = new float[1024];
            for (int i = 0; i < _hL.Length; i++)
            {
                hPL[i] = _hL[i];
                hPR[i] = _hR[i];
            }
            (_HReL, _HImL) = FFTProcess(hPL, new float[hPL.Length]);
            (_HReR, _HImR) = FFTProcess(hPR, new float[hPR.Length]);
            Array.Copy(_HReL, _HtReL, 1024);
            Array.Copy(_HImL, _HtImL, 1024);
            Array.Copy(_HReR, _HtReR, 1024);
            Array.Copy(_HImR, _HtImR, 1024);
            _filterPrimed = true;
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

        #region FFTs
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

        public static (float[], float[]) IFFTProcess(float[] re, float[] im)
        {
            (float[] outRe, float[] outIm) = IFFTProcessRecursive(re, im);
            for (int i = 0; i < outRe.Length; i++)
            {
                outRe[i] /= outRe.Length;
                outIm[i] /= outIm.Length;
            }
            return (outRe, outIm);
        }

        public static (float[], float[]) IFFTProcessRecursive(float[] re, float[] im)
        {
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
            (float[] eR, float[] Ei) = IFFTProcessRecursive(eRe, eIm);
            (float[] oR, float[] oI) = IFFTProcessRecursive(oRe, oIm);

            float[] outRe = new float[re.Length];
            float[] outIm = new float[re.Length];

            //combine and do the final calculation
            for (int k = 0; k < re.Length / 2; k++)
            {
                //angle calculation/the shift for the point
                float wR = MathF.Cos(2f * MathF.PI * k / re.Length);
                float wI = MathF.Sin(2f * MathF.PI * k / re.Length);
                //shift the points 
                float tR = wR * oR[k] - wI * oI[k];
                float tI = wR * oI[k] + wI * oR[k];
                //combine the even and odd parts for the first half, evens are the baseline to measure the shift of odd's
                outRe[k] = eR[k] + tR;
                outIm[k] = Ei[k] + tI;
                //fill out the 2nd half of the data since its 180 degree flip
                //cos(x + 180) = -cos(x), sin(x+180) = -sin(x), and tR is x coord tI is y cord, (cos,sin)
                outRe[k + re.Length / 2] = eR[k] - tR;
                outIm[k + re.Length / 2] = Ei[k] - tI;
            }
            return (outRe, outIm);
        }
        public static void FFTProcessIter(float[] re, float[] im)
        {
            if (re.Length > 1)
            {
                int lvl = BitOperations.Log2((uint)re.Length);
                //Bit flip/shift the index/swap values to prep the array
                for (int i = 0; i < re.Length; i++)
                {
                    int reversed = 0;
                    int input = i;
                    for (int b = 0; b < lvl; b++)
                    {
                        reversed = (reversed << 1) | (input & 1);
                        input >>= 1;
                    }

                    if (reversed > i)
                    {
                        (re[i], re[reversed]) = (re[reversed], re[i]);
                        (im[i], im[reversed]) = (im[reversed], im[i]);
                    }
                }

                // Start in pairs of 2 and then go up * 2
                int group = 2;
                while (group <= re.Length)
                {
                    int step = group / 2; // Step count is half the group length(group of 8 steps 4 ahead, 0,1,2,3 and 4,5,6,7 so 0-4,1-5,2-6,3-7)
                    for (int block = 0; block < re.Length; block += group)
                    {
                        for (int k = 0; k < step; k++)
                        {
                            int tick = k * (1024 / group);
                            float wR = _twRe[tick];
                            float wI = _twIm[tick];

                            int iEven = block + k;
                            int iOdd = block + k + step;

                            float evenR = re[iEven];// store old values
                            float evenI = im[iEven];

                            // rotate points
                            float tR = wR * re[iOdd] - wI * im[iOdd];
                            float tI = wR * im[iOdd] + wI * re[iOdd];

                            // apply rotations
                            re[iOdd] = evenR - tR;
                            im[iOdd] = evenI - tI;

                            re[iEven] = evenR + tR;
                            im[iEven] = evenI + tI;
                        }
                    }
                    group *= 2;
                }
            }
        }

        public static void IFFTProcessIter(float[] re, float[] im)
        {
            if (re.Length > 1)
            {
                //Bit flip/shift the index/swap values to prep the array
                int lvl = BitOperations.Log2((uint)re.Length);
                for (int i = 0; i < re.Length; i++)
                {
                    int reversed = 0;
                    int input = i;
                    for (int b = 0; b < lvl; b++)
                    {
                        reversed = (reversed << 1) | (input & 1);
                        input >>= 1;
                    }

                    if (reversed > i)
                    {
                        (re[i], re[reversed]) = (re[reversed], re[i]);
                        (im[i], im[reversed]) = (im[reversed], im[i]);
                    }
                }

                // Start in pairs of 2 and then go up * 2
                int group = 2;
                while (group <= re.Length)
                {
                    int step = group / 2; // Step count is half the group length(group of 8 steps 4 ahead, 0,1,2,3 and 4,5,6,7 so 0-4,1-5,2-6,3-7)
                    for (int block = 0; block < re.Length; block += group)
                    {
                        for (int k = 0; k < step; k++)
                        {
                            int tick = k * (1024 / group);
                            float wR = _twRe[tick];
                            float wI = -_twIm[tick];

                            int iEven = block + k;
                            int iOdd = block + k + step;

                            float evenR = re[iEven];
                            float evenI = im[iEven];

                            // rotate points
                            float tR = wR * re[iOdd] - wI * im[iOdd];
                            float tI = wR * im[iOdd] + wI * re[iOdd];

                            // apply rotations
                            re[iOdd] = evenR - tR;
                            im[iOdd] = evenI - tI;

                            re[iEven] = evenR + tR;
                            im[iEven] = evenI + tI;
                        }
                    }
                    group *= 2;
                }
                // apply /N normalization during processing, take over from caller handling it
                for (int i = 0; i < re.Length; i++)
                {
                    re[i] /= re.Length;
                    im[i] /= re.Length;
                }
            }
        }
        #endregion
        public static float[] OverlapAdd(float[] h, float[] x, int blockSize)
        {
            float[] output = new float[x.Length + h.Length - 1];
            float[] hRe = new float[1024];
            float[] hIm = new float[1024];
            for (int i = 0; i < h.Length; i++) hRe[i] = h[i];
            FFTProcessIter(hRe, hIm);
            for (int b = 0; b < x.Length; b += blockSize)
            {
                float[] xRe = new float[1024];
                float[] xIm = new float[1024];
                Array.Copy(x, b, xRe, 0, blockSize);
                FFTProcessIter(xRe, xIm);
                float[] yR = new float[hRe.Length];
                float[] yI = new float[hRe.Length];
                for (int i = 0; i < yR.Length; i++)
                {
                    yR[i] = hRe[i] * xRe[i] - hIm[i] * xIm[i];
                    yI[i] = hRe[i] * xIm[i] + hIm[i] * xRe[i];
                }
                (float[] yT, float[] yTI) = IFFTProcess(yR, yI);
                for(int i = 0; i < 1024; i++)
                {
                    if(b+i < output.Length) output[b + i] += yT[i];
                }
            }

            return output;
        }

        public static void OLAProcess(float[] x, float[] dest)
        {
            Array.Clear(dest,0, dest.Length);
            int frames = x.Length / 2;
            if (frames % 480 != 0) throw new InvalidDataException("Data stream was not divisible by 480 chunks");
            int blockSize = 480;
            for (int blockOffset = 0; blockOffset < frames; blockOffset += blockSize)
            {
                const float alpha = 0.15f;
                for (int k = 0; k < 1024; k++)
                {
                    _HReL[k] += (_HtReL[k] - _HReL[k]) * alpha;
                    _HImL[k] += (_HtImL[k] - _HImL[k]) * alpha;
                    _HReR[k] += (_HtReR[k] - _HReR[k]) * alpha;
                    _HImR[k] += (_HtImR[k] - _HImR[k]) * alpha;
                }
                //L ear
                Array.Clear(_block, 0, _block.Length);
                Array.Clear(_blockIm, 0, _blockIm.Length);
                for (int f = 0; f < 480; f++) _block[f] = x[2*(blockOffset + f)];
                FFTProcessIter(_block, _blockIm);
                for (int k = 0; k < _ffReL.Length; k++)
                {
                    _ffReL[k] = _HReL[k] * _block[k] - _HImL[k] * _blockIm[k];
                    _ffImL[k] = _HReL[k] * _blockIm[k] + _HImL[k] * _block[k];
                }
                IFFTProcessIter(_ffReL, _ffImL);
                for (int i = 0; i < _accL.Length; i++) _accL[i] += _ffReL[i];
                for (int f = 0; f < 480; f++) dest[2 * (blockOffset + f)] = _accL[f] * 2;
                for (int k = 0; k < 544; k++) _accL[k] = _accL[k + 480];
                Array.Clear(_accL, 544, _accL.Length - 544);

                //R ear
                Array.Clear(_block, 0, _block.Length);
                Array.Clear(_blockIm, 0, _blockIm.Length);
                for (int f = 0; f < 480; f++) _block[f] = x[2 * (blockOffset + f) + 1];
                FFTProcessIter(_block, _blockIm);
                for (int k = 0; k < _ffReR.Length; k++)
                {
                    _ffReR[k] = _HReR[k] * _block[k] - _HImR[k] * _blockIm[k];
                    _ffImR[k] = _HReR[k] * _blockIm[k] + _HImR[k] * _block[k];
                }
                IFFTProcessIter(_ffReR, _ffImR);
                for (int i = 0; i < _accR.Length; i++) _accR[i] += _ffReR[i];
                for (int f = 0; f < 480; f++) dest[2 * (blockOffset + f) + 1] = _accR[f] * 2;
                for (int k = 0; k < 544; k++) _accR[k] = _accR[k + 480];
                Array.Clear(_accR, 544, _accR.Length - 544);
            }
        }

        public static float[] Resample(float[] srcIr, double srcRate, double destRate)
        {
            double ratio = destRate / srcRate;

            int outLen = (int)Math.Floor(srcIr.Length * ratio);
            float[] outIR = new float[outLen];

            for (int i = 0; i < outLen; i++) {
                double srcPos = i / ratio;

                int left = (int)Math.Floor(srcPos);
                int right = left + 1;

                float frac = (float)(srcPos - left);
                if (right >= srcIr.Length) outIR[i] = srcIr[left];
                else outIR[i] = (srcIr[left] * (1.0f - frac)) + (srcIr[right] * frac);
            }

            return outIR;
        }

        public static void LoadAllHRTF(int ele)
        {
            for(int azIndex = 0; azIndex < 72; azIndex++)
            {
                float[] hL = HrtfDatabase.GetIr(ele, azIndex*5, "L");
                float[] hR = HrtfDatabase.GetIr(ele, azIndex*5, "R");

                hL = Resample(hL, 44100.0, 48000.0);
                hR = Resample(hR, 44100.0, 48000.0);
                hL = hL[0..512];
                hR = hR[0..512];

                float sL = hL.Sum(a => MathF.Abs(a));
                float sR = hR.Sum(a => MathF.Abs(a));
                for (int i = 0; i < hL.Length; i++)
                {
                    hL[i] /= sL;
                    hR[i] /= sR;
                }

                float[] phL = new float[1024];
                float[] phR = new float[1024];
                for(int i = 0;i < hL.Length; i++)
                {
                    phL[i] = hL[i];
                    phR[i] = hR[i];
                }
                (_tableHReL[azIndex], _tableHImL[azIndex]) = FFTProcess(phL, new float[phL.Length]);
                (_tableHReR[azIndex], _tableHImR[azIndex]) = FFTProcess(phR, new float[phR.Length]);
            }
        }

        public static void Reset()
        {
            Array.Clear(_accL, 0, _accL.Length);
            Array.Clear(_accR, 0, _accR.Length);
            Array.Clear(_hrtfRingL, 0, _hrtfRingL.Length);
            Array.Clear(_hrtfRingR, 0, _hrtfRingR.Length);
            _hrtfPosL = 0;
            _hrtfPosR = 0;
        }

        public static float PeakLevel(float[] interleaved, int channel)
        {
            float m = 0;
            for(int i = 0; i < interleaved.Length / 2;i++)
            {
                m = MathF.Max(MathF.Abs(m), MathF.Abs(interleaved[(2 * i) + channel]));
            }
            return m;
        }

        public static void GetHRTFMagnitude(float[] magL, float[] magR)
        { 
            for(int i = 0; i < magL.Length; i++)
            {
                magL[i] = MathF.Sqrt(MathF.Pow(_HImL[i],2) + MathF.Pow(_HReL[i], 2));
                magR[i] = MathF.Sqrt(MathF.Pow(_HImR[i], 2) + MathF.Pow(_HReR[i], 2));
            }
        }

        static Spatializer()
        {
            for(int i = 0; i < 512; i++)
            {
                float angle = -2f * MathF.PI * i / 1024;
                _twRe[i] = MathF.Cos(angle);
                _twIm[i] = MathF.Sin(angle);
            }
            if(HrtfDatabase.IsAvailable()) LoadAllHRTF(0);
        }
    }
}
