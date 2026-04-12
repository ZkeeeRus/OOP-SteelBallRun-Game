using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace SBR_Game.Rendering
{
    public class Shader
    {
        public int Handle { get; private set; }

        private readonly Dictionary<string, int> _uniformLocations = new();

        public Shader(string vertexPath, string fragmentPath) => Init(File.ReadAllText(vertexPath), File.ReadAllText(fragmentPath));

        public Shader(string vertexSource, string fragmentSource, bool fromMemory) => Init(vertexSource, fragmentSource);

        private void Init(string vertexSource, string fragmentSource)
        {
            int vert = CreateShader(ShaderType.VertexShader, vertexSource);
            int frag = CreateShader(ShaderType.FragmentShader, fragmentSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vert);
            GL.AttachShader(Handle, frag);
            GL.LinkProgram(Handle);
            GL.DetachShader(Handle, vert);
            GL.DetachShader(Handle, frag);
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            CacheUniformLocations();
            GL.UseProgram(Handle);

            if (_uniformLocations.TryGetValue("uTexture", out int texLoc)) GL.Uniform1(texLoc, 0);
            if (_uniformLocations.TryGetValue("uColor", out int colLoc)) GL.Uniform4(colLoc, 1f, 1f, 1f, 1f);
        }

        private void CacheUniformLocations()
        {
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int count);
            for (int i = 0; i < count; i++)
            {
                string name = GL.GetActiveUniform(Handle, i, out _, out _);
                _uniformLocations[name] = GL.GetUniformLocation(Handle, name);
            }
        }

        private static int CreateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            return shader;
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetInt(string name, int value)
        {
            if (_uniformLocations.TryGetValue(name, out int loc))
                GL.Uniform1(loc, value);
        }

        public void SetColor(string name, Color value)
        {
            if (_uniformLocations.TryGetValue(name, out int loc))
                GL.Uniform4(loc, value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f);
        }
    }
}
