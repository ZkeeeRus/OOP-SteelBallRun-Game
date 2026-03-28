using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SBR_Game.Rendering
{
    public class DebugRenderer : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const float DebounceTime = 200f;
        private const int LineHeight = 25;
        private const int Margin = 10;

        private bool _showFps;
        private bool _showInfo;
        private bool _showPolygons;
        private int _frameCount;
        private float _fps;
        private float _fpsTimer;
        private string _fpsText = "FPS: 0";
        private string _infoText = "";

        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _lineVao;
        private int _lineVbo;
        private Shader _shader;
        private Shader _lineShader;
        private bool _initialized;

        private float _lastF1Time;
        private float _lastF2Time;
        private float _lastF3Time;

        public bool ShowFps { get; set; }
        public bool ShowInfo { get; set; }
        public bool ShowPolygons { get; set; }

        public void Initialize()
        {
            // ✅ НЕ грузи vertex.glsl/fragment.glsl здесь — это шейдер рендерера
            // _shader = new Shader(Path.Combine(basePath, "vertex.glsl"), ...);  <-- УБЕРИ

            // Для текста дебаггера нужен отдельный простой шейдер:
            string textVertexSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec2 aTexCoord;
        out vec2 vTexCoord;
        void main() {
            vTexCoord = aTexCoord;
            gl_Position = vec4(aPosition, 1.0);
        }";

            string textFragmentSource = @"
        #version 330 core
        in vec2 vTexCoord;
        uniform sampler2D uTexture;
        out vec4 FragColor;
        void main() {
            FragColor = texture(uTexture, vTexCoord);
        }";

            _shader = new Shader(textVertexSource, textFragmentSource, fromMemory: true);

            // Устанавливаем uTexture = 0 сразу
            _shader.Use();
            int texLoc = GL.GetUniformLocation(_shader.Handle, "uTexture");
            GL.Uniform1(texLoc, 0);

            // Линейный шейдер для wireframe — без изменений
            string lineVertexSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        void main() {
            gl_Position = vec4(aPosition, 1.0);
        }";

            string lineFragmentSource = @"
        #version 330 core
        out vec4 FragColor;
        uniform vec4 uColor;
        void main() {
            FragColor = uColor;
        }";

            _lineShader = new Shader(lineVertexSource, lineFragmentSource, fromMemory: true);

            // VAO/VBO инициализация — без изменений
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

            _lineVao = GL.GenVertexArray();
            GL.BindVertexArray(_lineVao);
            _lineVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            _initialized = true;
        }

        public void Update(float deltaTime, int objectCount, int vertexCount)
        {
            _frameCount++;
            _fpsTimer += deltaTime;

            if (_fpsTimer >= 1f)
            {
                _fps = _frameCount / _fpsTimer;
                _fpsText = $"FPS: {_fps:F1}";
                _frameCount = 0;
                _fpsTimer = 0f;
            }

            _infoText = $"Objects: {objectCount} | Vertices: {vertexCount}";

            HandleInput(deltaTime);
        }

        private void HandleInput(float deltaTime)
        {
            _lastF1Time += deltaTime * 1000;
            _lastF2Time += deltaTime * 1000;
            _lastF3Time += deltaTime * 1000;

            if ((GetAsyncKeyState((int)Keys.F1) & 0x8000) != 0 && _lastF1Time >= DebounceTime)
            {
                _showPolygons = !_showPolygons;
                _lastF1Time = 0;
            }

            if ((GetAsyncKeyState((int)Keys.F2) & 0x8000) != 0 && _lastF2Time >= DebounceTime)
            {
                _showFps = !_showFps;
                _lastF2Time = 0;
            }

            if ((GetAsyncKeyState((int)Keys.F3) & 0x8000) != 0 && _lastF3Time >= DebounceTime)
            {
                _showInfo = !_showInfo;
                _lastF3Time = 0;
            }
        }

        public void Render(float screenWidth, float screenHeight, List<GameObject> objects)
        {
            if (!_initialized) return;

            if (_showPolygons)
                RenderPolygons(objects, screenWidth, screenHeight);

            if (_showFps || _showInfo)
                RenderText(screenWidth, screenHeight);
        }

        private void RenderPolygons(List<GameObject> objects, float screenWidth, float screenHeight)
        {
            // ✅ Убрали GL.Disable/Enable DepthTest
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindVertexArray(_lineVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVbo);

            foreach (var obj in objects)
            {
                obj.GetVertices(screenWidth, screenHeight, out float[] vertices, out _);

                float[] positions = new float[12];
                for (int i = 0; i < 4; i++)
                {
                    positions[i * 3 + 0] = vertices[i * 5 + 0];
                    positions[i * 3 + 1] = vertices[i * 5 + 1];
                    positions[i * 3 + 2] = vertices[i * 5 + 2];
                }

                DrawTriangleWireframe(positions, Color.Red);
                DrawBoundingBox(obj, screenWidth, screenHeight, Color.Lime);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }   

        private void DrawTriangleWireframe(float[] positions, Color color)
        {
            _lineShader.Use();
            _lineShader.SetColor("uColor", color);

            float[] tri1 = [
                positions[0], positions[1], positions[2],
                positions[3], positions[4], positions[5],
                positions[3], positions[4], positions[5],
                positions[9], positions[10], positions[11],
                positions[9], positions[10], positions[11],
                positions[0], positions[1], positions[2]
            ];

            float[] tri2 = [
                positions[3], positions[4], positions[5],
                positions[6], positions[7], positions[8],
                positions[6], positions[7], positions[8],
                positions[9], positions[10], positions[11],
                positions[9], positions[10], positions[11],
                positions[3], positions[4], positions[5]
            ];

            GL.BufferData(BufferTarget.ArrayBuffer, tri1.Length * sizeof(float), tri1, BufferUsageHint.DynamicDraw);
            GL.DrawArrays(PrimitiveType.Lines, 0, 6);

            GL.BufferData(BufferTarget.ArrayBuffer, tri2.Length * sizeof(float), tri2, BufferUsageHint.DynamicDraw);
            GL.DrawArrays(PrimitiveType.Lines, 0, 6);
        }

        private void DrawBoundingBox(GameObject obj, float screenWidth, float screenHeight, Color color)
        {
            _lineShader.Use();
            _lineShader.SetColor("uColor", color);

            float halfWidth = obj.Width / 2f;
            float halfHeight = obj.Height / 2f;

            // ✅ Правильная формула — та же что в GetVertices
            float ndcLeft = ((obj.Position.X - halfWidth) / screenWidth) * 2f - 1f;
            float ndcRight = ((obj.Position.X + halfWidth) / screenWidth) * 2f - 1f;
            float ndcBottom = ((obj.Position.Y - halfHeight) / screenHeight) * 2f - 1f;
            float ndcTop = ((obj.Position.Y + halfHeight) / screenHeight) * 2f - 1f;

            float[] rect =
            [
                ndcLeft,  ndcTop,    0f,
        ndcRight, ndcTop,    0f,
        ndcRight, ndcTop,    0f,
        ndcRight, ndcBottom, 0f,
        ndcRight, ndcBottom, 0f,
        ndcLeft,  ndcBottom, 0f,
        ndcLeft,  ndcBottom, 0f,
        ndcLeft,  ndcTop,    0f
            ];

            GL.BufferData(BufferTarget.ArrayBuffer, rect.Length * sizeof(float), rect, BufferUsageHint.DynamicDraw);
            GL.DrawArrays(PrimitiveType.Lines, 0, 8);
        }

        private void RenderText(float screenWidth, float screenHeight)
        {
            // ✅ Убрали GL.Disable/Enable DepthTest
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            int yOffset = Margin;

            if (_showFps)
            {
                DrawText(_fpsText, Margin, yOffset, Color.Lime, screenWidth, screenHeight);
                yOffset += LineHeight;
            }

            if (_showInfo)
                DrawText(_infoText, Margin, yOffset, Color.Yellow, screenWidth, screenHeight);
        }

        private void DrawText(string text, int x, int y, Color color, float screenWidth, float screenHeight)
        {
            using var bitmap = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bitmap);
            using var font = new Font("Arial", 16, FontStyle.Bold);
            var size = g.MeasureString(text, font);

            int width = (int)Math.Ceiling(size.Width);
            int height = (int)Math.Ceiling(size.Height);

            using var textureBitmap = new Bitmap(width, height);
            using var tg = Graphics.FromImage(textureBitmap);
            tg.Clear(Color.Transparent);
            tg.DrawString(text, font, new SolidBrush(color), 0, 0);

            var data = textureBitmap.LockBits(
                new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            textureBitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            float ndcLeft = (x / (screenWidth / 2f)) - 1f;
            float ndcRight = ((x + width) / (screenWidth / 2f)) - 1f;
            float ndcBottom = (y / (screenHeight / 2f)) - 1f;
            float ndcTop = ((y + height) / (screenHeight / 2f)) - 1f;

            float[] vertices = [
                ndcRight, ndcTop,    0.0f,  1.0f, 0.0f,
                ndcRight, ndcBottom, 0.0f,  1.0f, 1.0f,
                ndcLeft,  ndcBottom, 0.0f,  0.0f, 1.0f,
                ndcLeft,  ndcTop,    0.0f,  0.0f, 0.0f
            ];

            uint[] indices = [0, 1, 3, 1, 2, 3];

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            _shader.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.DeleteTexture(texture);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_lineVbo);
            GL.DeleteVertexArray(_lineVao);
        }
    }
}