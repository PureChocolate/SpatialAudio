using NAudio.CoreAudioApi;
using NAudio.Wave;
using SpatialAudio;
using System;

namespace Audio{
    class Program{
        static void Main(string[] args){
            WindowTracker.EnablePerMonitorDpiAwareness();
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
                    Console.CursorVisible = false;
                    Console.Clear();
                    while (true)
                    {
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                        Console.SetCursorPosition(0, 0);
                        var (az, dist, title) = WindowTracker.GetFocusedInfo();
                        Spatializer.CurrentAzimuthDeg = az;
                        Console.Write($"{title,-40} -> {az:F1} deg, {dist:F0}px".PadRight(110));
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