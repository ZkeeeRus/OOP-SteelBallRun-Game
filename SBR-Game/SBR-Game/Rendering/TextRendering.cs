using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace SBR_Game.Rendering
{
    public class TextRenderer : IDisposable
    {
        private Shader _shader = null!;
        private int _vao, _vbo, _ebo;
        private bool _initialized;

        public void Initialize()
        {
            _shader = new Shader(
                "#version 330 core\nlayout(location=0)in vec3 aPosition;layout(location=1)in vec2 aTexCoord;out vec2 vTexCoord;void main(){vTexCoord=aTexCoord;gl_Position=vec4(aPosition,1.0);}",
                "#version 330 core\nin vec2 vTexCoord;uniform sampler2D uTexture;out vec4 FragColor;void main(){FragColor=texture(uTexture,vTexCoord);}",
                fromMemory: true);

            InitBuffer(ref _vao, ref _vbo, ref _ebo);
            _initialized = true;
        }

        private static void InitBuffer(ref int vao, ref int vbo, ref int ebo)
        {
            vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
            vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            ebo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void DrawText(string text, int x, int y, Color color, float screenWidth, float screenHeight)
        {
            if (!_initialized || _disposed) return;
            using var font = new Font("Arial", 16, FontStyle.Bold);

            SizeF size;
            using (var tmpBmp = new Bitmap(1, 1))
            using (var mg = Graphics.FromImage(tmpBmp))
                size = mg.MeasureString(text, font);

            int w = (int)Math.Ceiling(size.Width);
            int h = (int)Math.Ceiling(size.Height);

            using var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.DrawString(text, font, new SolidBrush(color), 0, 0);
            }

            var imgData = bmp.LockBits(
                new Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                w, h, 0, PixelFormat.Bgra, PixelType.UnsignedByte, imgData.Scan0);
            bmp.UnlockBits(imgData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            float ndcL = (x / (screenWidth / 2f)) - 1f;
            float ndcR = ((x + w) / (screenWidth / 2f)) - 1f;
            float ndcT = 1f - (y / (screenHeight / 2f));
            float ndcB = 1f - ((y + h) / (screenHeight / 2f));

            float[] vertices =
            {
                ndcR, ndcT,    0f, 1f, 0f,
                ndcR, ndcB,    0f, 1f, 1f,
                ndcL, ndcB,    0f, 0f, 1f,
                ndcL, ndcT,    0f, 0f, 0f
            };
            uint[] indices = { 0, 1, 3, 1, 2, 3 };

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StreamDraw);

            _shader.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.DeleteTexture(tex);
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }
    }
}   