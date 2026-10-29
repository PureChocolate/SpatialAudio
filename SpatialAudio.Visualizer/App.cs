using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace SpatialAudio.Visualizer
{
    sealed class App : IDisposable
    {
        IWindow _window = null!;
        GL _gl = null!;
        IInputContext? _input = null;
        public static void Run()
        {
            using var app = new App();
            app._window = Window.Create(BuildOptions());
            app._window.Load += app.OnLoad;
            app._window.Render += app.OnRender;
            app._window.Closing += app.OnClosing;
            app._window.Run();

        }

        void OnLoad()
        {
            _gl = GL.GetApi(_window);
            _gl.ClearColor(0.05f, 0.06f, 0.09f, 1f);
            _input = _window.CreateInput();
            _input.Keyboards[0].KeyDown += OnKey;
        }

        void OnRender(double dt)
        {
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            if(dt > 0)
            {
                double fps = 1.0 / dt;
                _window.Title = $"SpatialAudio Visualizer | fps: {fps:F0}";
            }
        }

        void OnKey(IKeyboard keyboard, Key key, int scancode)
        {
            if (key == Key.Escape) _window.Close();
        }

        void OnClosing()
        {
            _input?.Dispose();
            _gl?.Dispose();
        }

        static WindowOptions BuildOptions()
        {
            var options = WindowOptions.Default with
            {
                Title = "SpatialAudio Visualizer",
                Size = new Silk.NET.Maths.Vector2D<int>(1280, 720),
                VSync = true,
                API = new GraphicsAPI(
                    ContextAPI.OpenGL,
                    ContextProfile.Core,
                    ContextFlags.ForwardCompatible,
                    new APIVersion(3, 3))
            };
            return options;
        }

        public void Dispose()
        { 
            _window?.Dispose();
        }
    }
}
