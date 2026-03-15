using SBR_Game.Rendering;
using System.Drawing;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace SBR_Game
{
    public partial class fMainWindow : Form
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly Renderer _renderer;
        private Vector3 _position = Vector3.Zero;
        private float _speed = 2.0f;
        private DateTime _lastTime;
        private bool _isReady;

        public fMainWindow()
        {
            InitializeComponent();
            _renderer = new Renderer();
            _lastTime = DateTime.Now;

            _updateTimer.Interval = 16;
            _updateTimer.Tick += OnTimerTick;
        }

        private void UpdateInput(float deltaTime)
        {
            if ((GetAsyncKeyState((int)Keys.W) & 0x8000) != 0) _position.Y += _speed * deltaTime;
            if ((GetAsyncKeyState((int)Keys.S) & 0x8000) != 0) _position.Y -= _speed * deltaTime;
            if ((GetAsyncKeyState((int)Keys.A) & 0x8000) != 0) _position.X -= _speed * deltaTime;
            if ((GetAsyncKeyState((int)Keys.D) & 0x8000) != 0) _position.X += _speed * deltaTime;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!_isReady || !glControl.Created) return;

            DateTime now = DateTime.Now;
            float deltaTime = (float)(now - _lastTime).TotalSeconds;
            _lastTime = now;

            UpdateInput(deltaTime);

            glControl.Invalidate();
        }

        private void glControl_Load(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            GL.ClearColor(Color.MidnightBlue);
            GL.Enable(EnableCap.DepthTest);

            _renderer.Initialize();

            this.Activate();
            this.BringToFront();
            glControl.Focus();
            this.ActiveControl = glControl;

            this.BeginInvoke(new Action(() => {
                _isReady = true;
                _updateTimer.Start();
            }));
        }

        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            glControl.MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _renderer.Render(_position);

            glControl.SwapBuffers();
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            if (glControl.ClientRectangle.Width > 0 && glControl.ClientRectangle.Height > 0)
            {
                glControl.MakeCurrent();
                _renderer.Resize(glControl.ClientRectangle.Width, glControl.ClientRectangle.Height);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _renderer.Dispose();
            base.OnFormClosed(e);
        }
    }
}