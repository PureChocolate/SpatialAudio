using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

namespace Audio{
    class Program{
        static void Main(string[] args){
            MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
            MMDeviceCollection endPoints = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            List<MMDevice> devices = new List<MMDevice>();
            int i = 1;
            foreach (MMDevice device in endPoints){   
                devices.Add(device);
                WaveFormat m = device.AudioClient.MixFormat;
                Console.WriteLine($"{i++}. {device.FriendlyName}, {device.DeviceFriendlyName}, {m.SampleRate}, {m.BitsPerSample}, {m.Channels}");
            }
            int c = -1;
            Console.WriteLine("Pick capture device:");
            if(int.TryParse( Console.ReadLine(), out c) && 1 <= c && c <= devices.Count){
                WasapiLoopbackCapture capture = new WasapiLoopbackCapture(devices[c-1]);
                Console.WriteLine("Pick output device:");
                int o = -1;
                if (int.TryParse(Console.ReadLine(), out o) && o != c && 1 <= o && o <= devices.Count){

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    double[] expectedMS = new double[10];
                    double[] recordedMS = new double[10];
                    bool[] matched = new bool[10];
                    double lastDetectionMS = double.MinValue;
                    bool reported = false;
                    int quietCount = 0;
                    int count = 0;
                    bool burstStartedQuiet = false;


                    BufferedWaveProvider bufferedWave = new BufferedWaveProvider(capture.WaveFormat);
                    bufferedWave.DiscardOnBufferOverflow = true;
                    capture.DataAvailable += onDataAvailable;

                    void onDataAvailable(object? sender, WaveInEventArgs e)
                    {
                        bufferedWave.AddSamples(e.Buffer, 0, e.BytesRecorded);
                        float[] chunk = new float[e.BytesRecorded / 4];
                        Buffer.BlockCopy(e.Buffer, 0, chunk, 0, e.BytesRecorded);


                        double nowMS = -1;
                        foreach (float f in chunk)
                        {
                            bool loud = Math.Abs(f) > 0.1f;
                            if (loud)
                            {
                                if (count == 0) burstStartedQuiet = quietCount >= 480;
                                count++;
                                quietCount = 0;
                                if (count == 16 && burstStartedQuiet)
                                {
                                    nowMS = watch.Elapsed.TotalMilliseconds;
                                }
                            }
                            else
                            {
                                count = 0;
                                burstStartedQuiet = false;
                                quietCount++;
                            }
                        }

                        if (nowMS < 0) return;
                        if (nowMS - lastDetectionMS < 300) return;
                        lastDetectionMS = nowMS;

                        for (int j = 0; j < matched.Length; j++)
                        {
                            if (Math.Abs(nowMS - expectedMS[j]) < 200 && !matched[j])
                            {
                                recordedMS[j] = nowMS - expectedMS[j];
                                matched[j] = true;
                                break;
                            }
                        }

                        if (matched.All(x => x) && !reported)
                        {
                            Console.WriteLine($"min: {recordedMS.Min():F1}, avg: {recordedMS.Average():F1}, max: {recordedMS.Max():F1}.");
                            reported = true;
                        }
                    }


                    capture.StartRecording();

                    WasapiOut output = new WasapiOut(devices[o-1], AudioClientShareMode.Shared, false, 100);
                    output.Init(bufferedWave);
                    output.Play();
                    
                    byte[] clickTrain = MakeClickTrain();
                    WasapiOut output2 = new WasapiOut(devices[c - 1], AudioClientShareMode.Shared, false, 50);
                    output2.Init(new RawSourceWaveStream(new MemoryStream(clickTrain), capture.WaveFormat));


                    double tPlay = watch.Elapsed.TotalMilliseconds;
                    output2.Play();
                    for(int k = 0; k < expectedMS.Length; k++)
                    {
                        expectedMS[k] = tPlay + (k * 500);
                    }

                    while (Console.ReadKey(true).Key != ConsoleKey.Escape){}

                    capture.StopRecording();
                    capture.Dispose();
                    output.Dispose();

                    output2.Stop();
                    output2.Dispose();
                }
                else { Console.WriteLine("Error: Not a number or not within listed range or is the same as capture device.");}

            }
            else { Console.WriteLine("Error: Not a number or not within listed range");}

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