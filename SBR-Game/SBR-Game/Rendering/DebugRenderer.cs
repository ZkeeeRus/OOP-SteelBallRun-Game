using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SBR_Game.Rendering
{
    public class DebugRenderer : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const float DebounceMs = 200f;
        private const int LineHeight = 25;
        private const int Margin = 10;

        private bool _showFps, _showInfo, _showPolygons;
        private int _frameCount;
        private float _fpsTimer;
        private string _fpsText = "FPS: 0";
        private string _infoText = "";

        private float _lastF1, _lastF2, _lastF3;

        private Shader _textShader = null!;
        private Shader _lineShader = null!;
        private int _textVao, _textVbo, _textEbo;
        private int _lineVao, _lineVbo;
        private bool _initialized;


        public void Initialize()
        {
            _textShader = new Shader(
                "#version 330 core\nlayout(location=0)in vec3 aPosition;\nlayout(location=1)in vec2 aTexCoord;\nout vec2 vTexCoord;\nvoid main(){vTexCoord=aTexCoord;gl_Position=vec4(aPosition,1.0);}",
                "#version 330 core\nin vec2 vTexCoord;\nuniform sampler2D uTexture;\nout vec4 FragColor;\nvoid main(){FragColor=texture(uTexture,vTexCoord);}",
                fromMemory: true);

            _lineShader = new Shader(
                "#version 330 core\nlayout(location=0)in vec3 aPosition;\nvoid main(){gl_Position=vec4(aPosition,1.0);}",
                "#version 330 core\nout vec4 FragColor;\nuniform vec4 uColor;\nvoid main(){FragColor=uColor;}",
                fromMemory: true);

            InitTexturedBuffer(ref _textVao, ref _textVbo, ref _textEbo);
            InitLineBuffer(ref _lineVao, ref _lineVbo);

            _initialized = true;
        }

        private static void InitTexturedBuffer(ref int vao, ref int vbo, ref int ebo)
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

        private static void InitLineBuffer(ref int vao, ref int vbo)
        {
            vao = GL.GenVertexArray(); GL.BindVertexArray(vao);
            vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }


        public void Update(float deltaTime, int objectCount, int vertexCount)
        {
            _frameCount++;
            _fpsTimer += deltaTime;

            if (_fpsTimer >= 1f)
            {
                _fpsText = $"FPS: {_frameCount / _fpsTimer:F1}";
                _frameCount = 0;
                _fpsTimer = 0f;
            }

            _infoText = $"Objects: {objectCount} | Vertices: {vertexCount}";
            HandleInput(deltaTime);
        }

        private void HandleInput(float deltaTime)
        {
            float ms = deltaTime * 1000f;
            _lastF1 += ms; _lastF2 += ms; _lastF3 += ms;

            if (IsKeyDown(Keys.F1) && _lastF1 >= DebounceMs) { _showPolygons = !_showPolygons; _lastF1 = 0; }
            if (IsKeyDown(Keys.F2) && _lastF2 >= DebounceMs) { _showFps = !_showFps; _lastF2 = 0; }
            if (IsKeyDown(Keys.F3) && _lastF3 >= DebounceMs) { _showInfo = !_showInfo; _lastF3 = 0; }
        }

        private static bool IsKeyDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;


        public void Render(float screenWidth, float screenHeight, List<GameObject> objects) => Render(0, 0, screenWidth, screenHeight, objects);

        public void Render(float vx, float vy, float vw, float vh, List<GameObject> objects)
        {
            if (!_initialized || _disposed) return;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (_showPolygons) RenderPolygons(objects, vx, vy, vw, vh);
            if (_showFps || _showInfo) RenderText(vw, vh);
        }


        private void RenderPolygons(List<GameObject> objects, float vx, float vy, float vw, float vh)
        {
            _lineShader.Use();
            GL.BindVertexArray(_lineVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVbo);

            foreach (var obj in objects)
            {
                obj.GetVertices(vx, vy, vw, vh, out float[] vertices, out _);

                float[] p = new float[12];
                for (int i = 0; i < 4; i++)
                {
                    p[i * 3] = vertices[i * 5];
                    p[i * 3 + 1] = vertices[i * 5 + 1];
                    p[i * 3 + 2] = vertices[i * 5 + 2];
                }

                DrawTriangleWireframe(p, Color.Red);
                DrawBoundingBox(obj, vx, vy, vw, vh, Color.Lime);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        private void DrawTriangleWireframe(float[] p, Color color)
        {
            _lineShader.SetColor("uColor", color);
            // Two triangles: (0,1,3) and (1,2,3) – each drawn as a loop
            DrawLineLoop(p[0], p[1], p[2], p[3], p[4], p[5], p[9], p[10], p[11]);
            DrawLineLoop(p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11]);
        }

        private void DrawLineLoop(
            float x0, float y0, float z0,
            float x1, float y1, float z1,
            float x2, float y2, float z2)
        {
            DrawLines(new float[]
            {
                x0, y0, z0,  x1, y1, z1,
                x1, y1, z1,  x2, y2, z2,
                x2, y2, z2,  x0, y0, z0
            });
        }

        private void DrawBoundingBox(GameObject obj, float vx, float vy, float vw, float vh, Color color)
        {
            _lineShader.SetColor("uColor", color);

            float halfW = obj.Width / 2f;
            float halfH = obj.Height / 2f;

            float ndcL = ((obj.Position.X - halfW - vx) / vw) * 2f - 1f;
            float ndcR = ((obj.Position.X + halfW - vx) / vw) * 2f - 1f;
            float ndcB = ((obj.Position.Y - halfH - vy) / vh) * 2f - 1f;
            float ndcT = ((obj.Position.Y + halfH - vy) / vh) * 2f - 1f;

            DrawLines(new float[]
            {
                ndcL, ndcT, 0f,  ndcR, ndcT, 0f,
                ndcR, ndcT, 0f,  ndcR, ndcB, 0f,
                ndcR, ndcB, 0f,  ndcL, ndcB, 0f,
                ndcL, ndcB, 0f,  ndcL, ndcT, 0f
            });
        }

        private void DrawLines(float[] data)
        {
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
            GL.DrawArrays(PrimitiveType.Lines, 0, data.Length / 3);
        }

        private void RenderText(float screenWidth, float screenHeight)
        {
            int yOffset = Margin;

            if (_showFps)
            {
                DrawText(_fpsText, Margin, yOffset, Color.Lime, screenWidth, screenHeight);
                yOffset += LineHeight;
            }
            if (_showInfo)
                DrawText(_infoText, Margin, yOffset, Color.Yellow, screenWidth, screenHeight);
        }

        public void DrawText(string text, int x, int y, Color color, float screenWidth, float screenHeight)
        {
            if (_disposed) return;
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

            GL.BindVertexArray(_textVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _textVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _textEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StreamDraw);

            _textShader.Use();
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
            GL.DeleteBuffer(_textVbo);
            GL.DeleteBuffer(_textEbo);
            GL.DeleteVertexArray(_textVao);
            GL.DeleteBuffer(_lineVbo);
            GL.DeleteVertexArray(_lineVao);
        }
    }
}