using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Runtime.InteropServices;

namespace SpatialAudio{
    class Program{
        const string Reset = "\x1b[0m";
        const string Dim = "\x1b[2m";
        const string Blue = "\x1b[34m";
        const string Green = "\x1b[32m";

        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr handle, uint mode);
        private static float[] chunk = new float[960];

        static void EnableAnsiColors()
        {
            IntPtr handle = GetStdHandle(-11);
            if (GetConsoleMode(handle, out uint mode))
            {
                SetConsoleMode(handle, mode | 0x0004);
            }
        }

        static void Main(string[] args){
            WindowTracker.EnablePerMonitorDpiAwareness();
            EnableAnsiColors();
            WindowTracker.ListMonitors();

            HrtfDatabase.ReadData();
            Console.WriteLine("Choose run: 1. Standard audio spatializer, 2. HRTF Testing, 3.FFT Testing");
            Spatializer.LoadHRTF(0, 270);

            int.TryParse(Console.ReadLine(), out int c);
            if(c == 1)
            {

                RunCapture();
            }
            else if(c == 2)
            {
                Spatializer.LoadHRTF(0, 45);
                // Card 1 verify — replay the KEMAR experiment
                float[] h0 = HrtfDatabase.GetIr(0, 45, "L");
                float[] hR0 = HrtfDatabase.GetIr(0, 45, "R");

                float sumH = h0.Sum(x => MathF.Abs(x));
                float sumHR = hR0.Sum(x => MathF.Abs(x));
                
                //normalize data sets.
                float[] h = sumH == 0 ? h0 : h0.Select(x => x / sumH).ToArray();
                float[] hR = sumHR == 0 ? hR0 : hR0.Select(x => x / sumHR).ToArray();

                //Card 3 test
                float[] nH = new float[512];
                nH[0] = 1f;
                nH[1] = 1f;
                float[] c0 = new float[512];
                Spatializer.HRTFProcess(nH, c0);
                Console.WriteLine("Test C y[0..7]: " + string.Join(", ", c0.Take(8).Select(f => f.ToString("E1"))));

                // Test A: impulse in -> h out (definition of impulse response)
                float[] impulse = new float[512];
                impulse[0] = 1f;
                float[] yA = new float[512];
                Spatializer.HRTFProcess(impulse, yA);
                Console.WriteLine("Test A y[0..7]: " + string.Join(", ", yA.Take(8).Select(f => f.ToString("E1"))));

                // Test B: click + half-strength echo
                float[] half = new float[512];
                half[0] = 1f;
                half[1] = 0.5f;
                float[] yB = new float[512];
                Spatializer.HRTFProcess(half, yB);
                Console.WriteLine("Test B y[0..2]: " + string.Join(", ", yB.Take(3).Select(f => f.ToString("E1"))));

                // Card 2 Test
                float[] impulse2 = new float[512];
                Array.Fill(impulse2, 1f);
                float[] a2 = new float[512];
                Spatializer.HRTFProcess(impulse2, a2);
                Console.WriteLine("Test A2 y[0..7]: " + string.Join(", ", a2.Take(8).Select(f => f.ToString("E1"))));

                float[] half2 = new float[512];
                float[] b2 = new float[512];
                Spatializer.HRTFProcess(half2, b2);
                Console.WriteLine("Test B2 y[0..2]: " + string.Join(", ", b2.Take(3).Select(f => f.ToString("E1"))));

                Console.WriteLine($"Sum H: {sumH}, Sum HR: {sumHR}");



                // Piece 1 regression — the loader must not have moved
                float[] data = HrtfDatabase.GetIr(40, 289, "L");
                Console.WriteLine($"Max value for L40e289a: {data.MaxBy(a => Math.Abs(a))}");
            } else if(c == 3)
            {
                float[][] x = new float[][]
                {
                    new float[8], new float[8], new float[8]
                };
                x[0][0] = 1f;
                for (int v = 0; v < x[0].Length; v++)
                {
                    x[1][v] = MathF.Cos(2 * MathF.PI * 1 * v / x[1].Length);
                    x[2][v] = MathF.Sin(2 * MathF.PI * 1 * v / x[1].Length);
                }
                foreach (float[] a in x)
                {
                    foreach (float p in a) Console.Write($"{p}, ");

                    Console.WriteLine("Next impulse");
                    for (int k = 0; k < a.Length; k++)
                    {
                        (float f, float g) = Spatializer.Probes(a, k);
                        // Piece 3 WIP — FFTProcess disabled (resume path in LEARNING.md)
                        (float[] ffRe, float[] ffIm) = Spatializer.FFTProcess(a, new float[a.Length]);
                        Console.WriteLine($"k: {k}, (F,G): ({f},{g}), plus |x| = {MathF.Sqrt((f * f) + (g * g))}");
                        Console.WriteLine($"k: {k}, (ffRe, ffIm): ({ffRe[k]},{ffIm[k]}) plus |x| = {MathF.Sqrt((ffRe[k] * ffRe[k]) + (ffIm[k] * ffIm[k]))}");
                    }
                }
                //float[] x = new float[512];
                //for(int n = 0; n < x.Length; n++)
                //{
                //    x[n] = MathF.Cos(2 * MathF.PI * 15 * ((float)n / x.Length));
                //}
                //(float f, float g) = Spatializer.Probes(x, 0);
                //Console.WriteLine($"k: {0}, (F,G): ({f},{g}), plus |x| = {MathF.Sqrt((f * f) + (g * g))}");
                //(f,g) = Spatializer.Probes(x, 15);
                //Console.WriteLine($"k: {15}, (F,G): ({f},{g}), plus |x| = {MathF.Sqrt((f * f) + (g * g))}");
                //(f,g) = Spatializer.Probes(x, 497);
                //Console.WriteLine($"k: {497}, (F,G): ({f},{g}), plus |x| = {MathF.Sqrt((f * f) + (g * g))}");

                
            }
        }
        // Audio generation test, kept for future reference.
        static byte[] MakeClickTrain() {
            WaveFormat wave = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

            float[] waveForm = new float[wave.SampleRate * wave.Channels * 5];

            for(int f  = 0; f < 240000; f++){
                if(f % 24000 < 960) {
                    float click = 0.5f * (MathF.Sin(2f * MathF.PI * 1000f * (f/48000f)));
                    waveForm[f * 2] = click;
                    waveForm[(f * 2) + 1] = click;
                }
                else {
                    waveForm[f * 2] = 0;
                    waveForm[(f * 2) + 1] = 0;
                }
            }

            byte[] bytes = new byte[waveForm.Length * 4];
            Buffer.BlockCopy(waveForm, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static void RunCapture()
        {
            MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
            MMDeviceCollection endPoints = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            List<MMDevice> devices = new List<MMDevice>();
            int i = 1;
            foreach (MMDevice device in endPoints)
            {
                devices.Add(device);
                WaveFormat m = device.AudioClient.MixFormat;
                Console.WriteLine($"{i++}. {device.FriendlyName}, {device.DeviceFriendlyName}, {m.SampleRate}, {m.BitsPerSample}, {m.Channels}");
            }
            int c = -1;
            Console.WriteLine("Pick capture device:");
            if (int.TryParse(Console.ReadLine(), out c) && 1 <= c && c <= devices.Count)
            {
                WasapiLoopbackCapture capture = new WasapiLoopbackCapture(devices[c - 1]);
                Console.WriteLine("Pick output device:");
                int o = -1;
                if (int.TryParse(Console.ReadLine(), out o) && o != c && 1 <= o && o <= devices.Count)
                {
                    //Capture buffer provider, sign up for notify with += for data available
                    BufferedWaveProvider bufferedWave = new BufferedWaveProvider(capture.WaveFormat);
                    bufferedWave.DiscardOnBufferOverflow = true;
                    capture.DataAvailable += OnDataAvailable;
                    void OnDataAvailable(object? sender, WaveInEventArgs e)
                    {
                        //480 frames of data, interlved so stereo channel = 960 floats/3840 Bytes
                        if (e.BytesRecorded != chunk.Length * 4) chunk = new float[e.BytesRecorded / 4];
                        Buffer.BlockCopy(e.Buffer, 0, chunk, 0, e.BytesRecorded);
                        bufferedWave.AddSamples(Spatializer.Process(chunk, capture.WaveFormat.SampleRate, Spatializer.CurrentAzimuthDeg), 0, e.BytesRecorded);
                    }

                    capture.StartRecording();

                    WasapiOut output = new WasapiOut(devices[o - 1], AudioClientShareMode.Shared, false, 100);
                    output.Init(bufferedWave);
                    output.Play();
                    Console.WriteLine();
                    Console.WriteLine($"{Dim}CAPTURE: {devices[c - 1].FriendlyName}  ->  OUTPUT: {devices[o - 1].FriendlyName}{Reset}");
                    Console.WriteLine();

                    int readoutRow = Console.CursorTop;
                    Console.CursorVisible = false;
                    string lastLine = "";
                    while (true)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                        var (az, dist, title) = WindowTracker.GetFocusedInfo();
                        Spatializer.CurrentAzimuthDeg = az;
                        if (title.Length > 40) title = title.Substring(0, 40);
                        string color = az < 0 ? Blue : Green;
                        string side = az < 0 ? "left" : "right";
                        string visibleLine = $"{title,-40} -> {az:F1} deg {side}, {dist:F0}px";
                        string line = $"{title,-40} -> {color}{az:F1} deg {side}{Reset}, {Dim}{dist:F0}px{Reset}";
                        int pad = 110 - visibleLine.Length;
                        if (pad > 0) line += new string(' ', pad);
                        if (line != lastLine)
                        {
                            Console.SetCursorPosition(0, readoutRow);
                            Console.Write(line);
                            lastLine = line;
                        }
                        Thread.Sleep(300);
                    }
                    Console.CursorVisible = true;
                    Console.WriteLine();

                    capture.StopRecording();
                    capture.Dispose();
                    output.Dispose();
                }
                else { Console.WriteLine("Error: Not a number or not within listed range or is the same as capture device."); }
            }
            else { Console.WriteLine("Error: Not a number or not within listed range"); }
        }

    }
}