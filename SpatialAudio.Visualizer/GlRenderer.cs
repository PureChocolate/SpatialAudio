using Silk.NET.OpenGL;

namespace SpatialAudio.Visualizer
{
    sealed class GlRenderer : IDisposable
    {
        GL _gl = null!;
        uint _program;
        uint _vao;
        uint _vbo;



        public void Init(GL gl)
        {
            _gl = gl;
            _program = BuildProgram("quad.vert", "quad.frag");
            SetupQuad();
        }

        private void SetupQuad()
        {
            float[] vertices = {
                -0.5f, -0.5f, 0.5f, -0.5f, 0.5f, 0.5f,
                -0.5f, -0.5f, 0.5f, 0.5f, -0.5f, 0.5f};

            _vao = _gl.CreateVertexArray();
            _gl.BindVertexArray(_vao);
            _vbo = _gl.CreateBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (uint)(vertices.Length * sizeof(float)),
                new ReadOnlySpan<float>(vertices),
                BufferUsageARB.StaticDraw);

            _gl.VertexAttribPointer(
                0, 2, VertexAttribPointerType.Float, false,
                2 * sizeof(float), IntPtr.Zero);
            _gl.EnableVertexAttribArray(0);

            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        public void Render()
        {
            _gl.UseProgram(_program);
            _gl.BindVertexArray(_vao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            _gl.BindVertexArray(0);
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
            _gl.DeleteBuffer(_vbo);
            _gl.DeleteVertexArray(_vao);

        }
    }
}
