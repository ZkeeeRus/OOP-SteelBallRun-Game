using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.IO;
using System.Reflection.Metadata;

namespace SBR_Game.Rendering
{
    public class Renderer : IDisposable
    {
        private const float BaseWidth = 1280f;
        private const float BaseHeight = 720f;

        private Shader _shader;
        private int _vao, _vbo, _ebo;
        private bool _disposed;
        private float _screenWidth;
        private float _screenHeight;

        public float ScreenWidth => _screenWidth;
        public float ScreenHeight => _screenHeight;
        public float ScaleX => _screenWidth / BaseWidth;
        public float ScaleY => _screenHeight / BaseHeight;

        public void Initialize()
        {
            // ✅ Грузим из файлов как ты и хотел
            string basePath = Path.Combine(AppContext.BaseDirectory, "Content", "Shaders");
            _shader = new Shader(
                Path.Combine(basePath, "vertex.glsl"),
                Path.Combine(basePath, "fragment.glsl"));

            InitBuffers();

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        private void InitBuffers()
        {
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void SetScreenSize(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            GL.Viewport(0, 0, width, height);
        }

        public void Render(List<GameObject> objects)
        {
            if (_screenWidth == 0 || _screenHeight == 0) return;

            _shader.Use();

            foreach (var obj in objects)
            {
                if (obj.Texture == null) continue;

                GL.ActiveTexture(TextureUnit.Texture0);
                obj.Texture.Use();
                _shader.SetInt("uTexture", 0);

                // ✅ uOffset убран — позиция уже в NDC
                obj.GetVertices(_screenWidth, _screenHeight, out float[] vertices, out uint[] indices);

                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                GL.BufferData(BufferTarget.ArrayBuffer,
                    vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer,
                    indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);

                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }

            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteBuffer(_vbo);
                GL.DeleteBuffer(_ebo);
                GL.DeleteVertexArray(_vao);
                _disposed = true;
            }
        }
    }
}