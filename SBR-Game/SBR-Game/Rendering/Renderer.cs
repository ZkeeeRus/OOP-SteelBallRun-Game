using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace SBR_Game.Rendering
{
    public class Renderer : IDisposable
    {
        private const float BaseWidth = 1280f;
        private const float BaseHeight = 720f;

        private Shader _shader = null!;
        private int _vao, _vbo, _ebo;

        private Shader _solidShader = null!;
        private int _solidVao, _solidVbo, _solidEbo;

        private bool _disposed;

        private float _screenWidth;
        private float _screenHeight;

        public float ScreenWidth => _screenWidth;
        public float ScreenHeight => _screenHeight;
        public float ScaleX => _screenWidth / BaseWidth;
        public float ScaleY => _screenHeight / BaseHeight;


        public void Initialize()
        {
            string shaderDir = Path.Combine(AppContext.BaseDirectory, "Content", "Shaders");
            _shader = new Shader(
                Path.Combine(shaderDir, "vertex.glsl"),
                Path.Combine(shaderDir, "fragment.glsl"));

            _solidShader = new Shader(
                "#version 330 core\nlayout(location=0)in vec3 aPosition;\nvoid main(){gl_Position=vec4(aPosition,1.0);}",
                "#version 330 core\nout vec4 FragColor;\nuniform vec4 uColor;\nvoid main(){FragColor=uColor;}",
                fromMemory: true);

            InitTexturedBuffer(ref _vao, ref _vbo, ref _ebo);
            InitSolidBuffer(ref _solidVao, ref _solidVbo, ref _solidEbo);

            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void SetScreenSize(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
        }


        public void Render(IEnumerable<GameObject> objects,
            int viewportX = 0, int viewportY = 0,
            int viewportWidth = 0, int viewportHeight = 0)
        {
            if (_screenWidth == 0 || _screenHeight == 0) return;

            int vw = viewportWidth > 0 ? viewportWidth : (int)Math.Round(_screenWidth);
            int vh = viewportHeight > 0 ? viewportHeight : (int)Math.Round(_screenHeight);
            if (vw <= 0 || vh <= 0) return;

            GL.Viewport(viewportX, viewportY, vw, vh);
            _shader.Use();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            foreach (var obj in objects)
            {
                if (obj.Texture == null) continue;

                GL.ActiveTexture(TextureUnit.Texture0);
                obj.Texture.Use();
                _shader.SetInt("uTexture", 0);
                _shader.SetColor("uColor", obj.Color);

                obj.GetVertices(viewportX, viewportY, vw, vh, out float[] vertices, out uint[] indices);

                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.DynamicDraw);
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }

            GL.BindVertexArray(0);
        }

        public void DrawSplitScreenDivider(float screenWidth, float screenHeight, float thicknessPx = 3f)
        {
            if (screenWidth <= 0 || screenHeight <= 0) return;

            float half = thicknessPx * 0.5f;
            float ndcBottom = ((screenHeight * 0.5f - half) / screenHeight) * 2f - 1f;
            float ndcTop = ((screenHeight * 0.5f + half) / screenHeight) * 2f - 1f;

            float[] verts = { 1f, ndcTop, 0f, 1f, ndcBottom, 0f, -1f, ndcBottom, 0f, -1f, ndcTop, 0f };
            uint[] idx = { 0, 1, 3, 1, 2, 3 };

            _solidShader.Use();
            _solidShader.SetColor("uColor", Color.White);

            GL.BindVertexArray(_solidVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _solidVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _solidEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idx.Length * sizeof(uint), idx, BufferUsageHint.DynamicDraw);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }


        private static void InitTexturedBuffer(ref int vao, ref int vbo, ref int ebo)
        {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

            // Position (3 floats) + TexCoord (2 floats)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        private static void InitSolidBuffer(ref int vao, ref int vbo, ref int ebo)
        {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

            // Position only (3 floats)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }


        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_solidVbo);
            GL.DeleteBuffer(_solidEbo);
            GL.DeleteVertexArray(_solidVao);
            GL.DeleteProgram(_solidShader.Handle);
        }
    }
}
