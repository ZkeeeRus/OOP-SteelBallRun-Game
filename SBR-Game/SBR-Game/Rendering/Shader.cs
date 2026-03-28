using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO;

namespace SBR_Game.Rendering
{
    public class Shader
    {
        public int Handle { get; private set; }
        private readonly Dictionary<string, int> _uniformLocations = new();

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexSource = File.ReadAllText(vertexPath);
            string fragmentSource = File.ReadAllText(fragmentPath);
            Init(vertexSource, fragmentSource);
        }

        public Shader(string vertexSource, string fragmentSource, bool fromMemory = true)
        {
            Init(vertexSource, fragmentSource);
        }

        private void Init(string vertexSource, string fragmentSource)
        {
            int vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string info = GL.GetProgramInfoLog(Handle);
                System.Diagnostics.Debug.WriteLine($"Program link error: {info}");
            }

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            CacheUniformLocations();

            // Устанавливаем sampler на unit 0
            GL.UseProgram(Handle);
            int texLoc = GL.GetUniformLocation(Handle, "uTexture");
            if (texLoc >= 0)
            {
                GL.Uniform1(texLoc, 0);
                System.Diagnostics.Debug.WriteLine($"uTexture location={texLoc}, set to unit 0");
            }
        }

        private void CacheUniformLocations()
        {
            // Получаем информацию о программе
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);

            for (int i = 0; i < uniformCount; i++)
            {
                string name = GL.GetActiveUniform(Handle, i, out _, out _);
                int location = GL.GetUniformLocation(Handle, name);
                _uniformLocations[name] = location;
                System.Diagnostics.Debug.WriteLine($"Uniform cached: {name} = {location}");
            }
        }

        private int CreateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                System.Diagnostics.Debug.WriteLine($"Shader compile error ({type}): {info}");
            }

            return shader;
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetInt(string name, int value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
            {
                GL.Uniform1(location, value);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Uniform {name} not found!");
            }
        }

        public void SetVector2(string name, OpenTK.Mathematics.Vector2 value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
            {
                GL.Uniform2(location, value.X, value.Y);
            }
        }

        public void SetVector3(string name, OpenTK.Mathematics.Vector3 value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
            {
                GL.Uniform3(location, value);
            }
        }

        public void SetColor(string name, Color value)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
            {
                GL.Uniform4(location, value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f);
            }
        }
    }
}