using NAudio.CoreAudioApi;
using NAudio.Wave;
using SpatialAudio;
using System;
using System.Runtime.InteropServices;

namespace Audio{
    class Program{
        const string RESET = "\x1b[0m";
        const string DIM = "\x1b[2m";
        const string BLUE = "\x1b[34m";
        const string GREEN = "\x1b[32m";

        [DllImport("kernel32.dll")]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr handle, uint mode);

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
                    BufferedWaveProvider bufferedWave = new BufferedWaveProvider(capture.WaveFormat);
                    bufferedWave.DiscardOnBufferOverflow = true;
                    capture.DataAvailable += onDataAvailable;

                    void onDataAvailable(object? sender, WaveInEventArgs e)
                    {
                        float[] chunk = new float[e.BytesRecorded / 4];
                        Buffer.BlockCopy(e.Buffer, 0, chunk, 0, e.BytesRecorded);
                        Spatializer.Process(chunk, capture.WaveFormat.SampleRate, Spatializer.CurrentAzimuthDeg);
                        byte[] processed = new byte[e.BytesRecorded];
                        Buffer.BlockCopy(chunk, 0, processed, 0, e.BytesRecorded);
                        bufferedWave.AddSamples(processed, 0, e.BytesRecorded);
                    }
                    
                    capture.StartRecording();

                    WasapiOut output = new WasapiOut(devices[o - 1], AudioClientShareMode.Shared, false, 100);
                    output.Init(bufferedWave);
                    output.Play();
                    Console.WriteLine();
                    Console.WriteLine($"{DIM}CAPTURE: {devices[c - 1].FriendlyName}  ->  OUTPUT: {devices[o - 1].FriendlyName}{RESET}");
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
                        string color = az < 0 ? BLUE : GREEN;
                        string side = az < 0 ? "left" : "right";
                        string visibleLine = $"{title,-40} -> {az:F1} deg {side}, {dist:F0}px";
                        string line = $"{title,-40} -> {color}{az:F1} deg {side}{RESET}, {DIM}{dist:F0}px{RESET}";
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

    }
}