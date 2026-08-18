using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

namespace Audio
{
    class Program
    {
        static void Main(string[] args)
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
            if(int.TryParse( Console.ReadLine(), out c) && 1 <= c && c <= devices.Count)
            {
                WasapiLoopbackCapture capture = new WasapiLoopbackCapture(devices[c-1]);
                Console.WriteLine("Pick output device:");
                int o = -1;
                if (int.TryParse(Console.ReadLine(), out o) && o != c && 1 <= o && o <= devices.Count)
                {
                    BufferedWaveProvider bufferedWave = new BufferedWaveProvider(capture.WaveFormat);
                    bufferedWave.DiscardOnBufferOverflow = true;
                    capture.DataAvailable += (s, e) => bufferedWave.AddSamples(e.Buffer, 0, e.BytesRecorded);

                    capture.StartRecording();

                    WasapiOut output = new WasapiOut(devices[o-1], AudioClientShareMode.Shared, false, 100);
                    output.Init(bufferedWave);
                    output.Play();
                    while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                    {

                    }

                    capture.StopRecording();
                    capture.Dispose();
                    output.Dispose();
                }
                else
                {
                    Console.WriteLine("Error: Not a number or not within listed range or is the same as capture device.");
                }

            }
            else
            {
                Console.WriteLine("Error: Not a number or not within listed range");
            }

        }
    }
}