using Silk.NET.OpenGL;

namespace SpatialAudio.Visualizer
{
    sealed class GlRenderer : IDisposable
    {
        GL _gl = null!;
        uint _program;

        public void Init(GL gl)
        {
            _gl = gl;
            _program = BuildProgram("quad.vert", "quad.frag");
        }

        uint BuildProgram(string nameV, string nameF)
        {
            string vert = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shaders", nameV));
            string frag = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shaders", nameF));
            uint a = _gl.CreateShader(ShaderType.VertexShader);
            uint b = _gl.CreateShader(ShaderType.FragmentShader);

            _gl.ShaderSource(a, vert);
            _gl.CompileShader(a);
            _gl.ShaderSource(b, frag);
            _gl.CompileShader(b);

            _gl.GetShader(a, ShaderParameterName.CompileStatus, out int ok);
            if (ok == 0) throw new  InvalidOperationException($"Shader compile failed: \n{_gl.GetShaderInfoLog(a)}");
            _gl.GetShader(b, ShaderParameterName.CompileStatus, out ok);
            if (ok == 0) throw new InvalidOperationException($"Shader compile failed: \n{_gl.GetShaderInfoLog(b)}");

            uint program = _gl.CreateProgram();
            _gl.AttachShader(program, a);
            _gl.AttachShader(program, b);

            _gl.LinkProgram(program);
            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out ok);
            if (ok == 0) throw new InvalidOperationException($"program link failed: \n{_gl.GetProgramInfoLog(program)}");

            _gl.DeleteShader(a);
            _gl.DeleteShader(b);
            return program;
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_program);
        }
    }
}
