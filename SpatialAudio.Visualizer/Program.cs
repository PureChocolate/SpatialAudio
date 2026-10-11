using SpatialAudio.Telemetry;
using MF = System.MathF;
using M = System.Math;

namespace SpatialAudio.Visualizer
{
    internal static class Program
    {
        static int _lastW = -1, _lastH = -1;
        static float _heldL, _heldR;
        static float meterDecay = 0.03f;
        static void Main()
        {
            try { Run(); }
            finally { Console.CursorVisible = true; }
        }

        static void Run()
        {
            Console.CursorVisible = false;
            Console.Clear();

            using var client = new TelemetryClient();
            bool running = true;

            while (running)
            {
                if (!client.Connect(500))
                {
                    ShowStatus("waiting for engine");
                    Thread.Sleep(300);
                    continue;
                }
                while (running)
                {
                    TelemetryFrame? f = null;
                    try { f = client.ReadFrame(); }
                    catch (IOException){ Console.WriteLine("IOException, reconnect"); }

                    if (f == null) break;

                    Render(f);

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) running = false;

                    Thread.Sleep(33);

                }   
            }
        }

        static void Render(TelemetryFrame f)
        {
            float rawL = f.LevelL, rawR = f.LevelR;          // true values, for the readout
            _heldL = MF.Max(rawL, _heldL - meterDecay);      // ballistics: snap up, decay down
            _heldR = MF.Max(rawR, _heldR - meterDecay);
            f.LevelL = _heldL;                               // bars = smoothed ABSOLUTE level
            f.LevelR = _heldR;

            if(Console.WindowWidth != _lastW || Console.WindowHeight != _lastH)
            {
                _lastH = Console.WindowHeight;
                _lastW = Console.WindowWidth;
                Console.Clear();
            }
            int width = Math.Clamp(Console.WindowWidth - 1, 40, 120);
            string[] lines = Renderer.RenderFrame(f, width);

            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < lines.Length; i++)
            {
                Console.Write(lines[i]);
                if (i < lines.Length - 1) Console.Write('\n');
            }
            Console.Write($"\n... L {rawL:F3} R {rawR:F3} ...");
        }

        static void ShowStatus(string status)
        {
            Console.SetCursorPosition(0, 0);
            Console.Write(status.PadRight(Math.Max(1, Console.WindowWidth - 1)));
        }
    }
}
