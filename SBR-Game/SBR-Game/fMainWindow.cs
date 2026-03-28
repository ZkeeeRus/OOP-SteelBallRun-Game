using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SBR_Game.Rendering;
using System.Drawing;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace SBR_Game
{
    public partial class fMainWindow : Form
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly Renderer _renderer;
        private readonly GameObjectFactory _factory;
        private readonly DebugRenderer _debug;
        private readonly List<GameObject> _objects;

        private GameObject _player;
        private ScrollingBackground _scrollingBg;
        private float _playerRelativeX;
        private DateTime _lastTime;
        private bool _isReady;

        private const float PlayerBaseWidth = 100f;
        private const float PlayerBaseHeight = 150f;

        public fMainWindow()
        {
            InitializeComponent();

            // ✅ Запрещаем произвольное изменение размера
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // Базовый размер окна под 16:9
            this.ClientSize = new Size(1280, 720);

            _renderer = new Renderer();
            _debug = new DebugRenderer();
            _objects = new List<GameObject>();
            _lastTime = DateTime.Now;

            string contentPath = Path.Combine(AppContext.BaseDirectory, "Content");
            _factory = new GameObjectFactory(contentPath);

            _updateTimer.Interval = 15;
            _updateTimer.Tick += OnTimerTick;

            glControl.KeyPress += (s, e) => e.Handled = true;
            KeyPreview = true;
        }

        private void UpdateInput(float deltaTime)
        {
            float speed = 1200f;

            if ((GetAsyncKeyState((int)Keys.A) & 0x8000) != 0)
                _scrollingBg.Scroll(-speed * deltaTime);
            if ((GetAsyncKeyState((int)Keys.D) & 0x8000) != 0)
                _scrollingBg.Scroll(speed * deltaTime);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!_isReady || !glControl.Created) return;

            float deltaTime = (float)(DateTime.Now - _lastTime).TotalSeconds;
            _lastTime = DateTime.Now;

            UpdateInput(deltaTime);
            _debug.Update(deltaTime, _objects.Count, _objects.Count * 4);
            glControl.Invalidate();
        }

        private void glControl_Load(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            GL.ClearColor(Color.MidnightBlue);

            _renderer.Initialize();
            _debug.Initialize();
            _renderer.SetScreenSize(glControl.Width, glControl.Height);

            // ✅ Папка с фонами
            string backgroundsPath = Path.Combine(
                AppContext.BaseDirectory, "Content", "Images", "Backgrounds");
            _scrollingBg = new ScrollingBackground(backgroundsPath);

            float scale = Math.Min(_renderer.ScaleX, _renderer.ScaleY);
            _player = _factory.CreateSprite(
                "player1.png",
                glControl.Width * 0.19f,
                glControl.Height * 0.05f,
                PlayerBaseWidth * scale,
                PlayerBaseHeight * scale,
                ScaleMode.KeepAspectRatio);

            

            _objects.Add(_player);

            _playerRelativeX = 0.5f;

            this.Activate();
            glControl.Focus();
            this.ActiveControl = glControl;

            this.BeginInvoke(() =>
            {
                _isReady = true;
                _updateTimer.Start();
            });
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            if (glControl.ClientRectangle.Width <= 0 ||
                glControl.ClientRectangle.Height <= 0) return;

            glControl.MakeCurrent();
            _renderer.SetScreenSize(glControl.Width, glControl.Height);

            if (_player != null)
            {
                // Берём минимальный масштаб чтобы персонаж не растягивался
                float scale = Math.Min(_renderer.ScaleX, _renderer.ScaleY);

                _player.Width = PlayerBaseWidth * scale;
                _player.Height = PlayerBaseHeight * scale;
                _player.Position = new Vector2(
                    glControl.Width * _playerRelativeX,
                    glControl.Height * 0.5f);
            }
        }

        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            glControl.MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (current, next) = _scrollingBg.GetSlides(glControl.Width, glControl.Height);
            var bgSlides = new List<GameObject> { current, next };

            _renderer.Render(bgSlides);
            _renderer.Render(_objects);

            // ✅ Передаём все объекты включая фон
            var allObjects = new List<GameObject>(bgSlides);
            allObjects.AddRange(_objects);
            _debug.Render(glControl.Width, glControl.Height, allObjects);

            glControl.SwapBuffers();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _renderer.Dispose();
            _debug.Dispose();
            _scrollingBg?.Dispose();
            foreach (var obj in _objects)
                obj.Texture?.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.WindowState == FormWindowState.Maximized)
            {
                // Подгоняем ClientSize под 16:9 в рамках экрана
                var screen = Screen.FromControl(this);
                int w = screen.WorkingArea.Width;
                int h = screen.WorkingArea.Height;

                float targetAspect = 16f / 9f;
                float screenAspect = (float)w / h;

                if (screenAspect > targetAspect)
                {
                    // Экран шире — ограничиваем по высоте
                    h = screen.WorkingArea.Height;
                    w = (int)(h * targetAspect);
                }
                else
                {
                    // Экран выше — ограничиваем по ширине
                    w = screen.WorkingArea.Width;
                    h = (int)(w / targetAspect);
                }

                this.ClientSize = new Size(w, h);
            }
        }
    }
}