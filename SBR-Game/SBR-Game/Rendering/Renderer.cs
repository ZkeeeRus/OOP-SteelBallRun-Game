using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.IO;

namespace SBR_Game.Rendering
{
    public class Renderer : IDisposable
    {
        private Shader _shader;
        private int _vao;
        private int _vbo;
        private bool _disposed;

        public void Initialize()
        {
            string base_path = Path.Combine(AppContext.BaseDirectory, "Content", "Shaders");
            string vertexPath = Path.Combine(base_path, "vertex.glsl");
            string fragmentPath = Path.Combine(base_path, "fragment.glsl");

            _shader = new Shader(vertexPath, fragmentPath);
            InitBuffers();
        }

        private void InitBuffers()
        {
            float[] vertices = new float[] {
                 0.0f,  0.5f, 0.0f,
                -0.5f, -0.5f, 0.0f,
                 0.5f, -0.5f, 0.0f
            };

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void Render(Vector3 offset)
        {
            _shader.Use();
            _shader.SetVector3("uOffset", offset);
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);
        }

        public void Resize(int width, int height)
        {
            GL.Viewport(0, 0, width, height);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteBuffer(_vbo);
                GL.DeleteVertexArray(_vao);
                _disposed = true;
            }
        }
    }
}