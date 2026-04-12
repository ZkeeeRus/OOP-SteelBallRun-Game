using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SBR_Game.Core;
using SBR_Game.Gameplay;
using SBR_Game.Gameplay.Bonuses;
using SBR_Game.Rendering;
using SBR_Game.States;
using SBR_Game.UI;
using System.Runtime.InteropServices;

namespace SBR_Game
{
    public partial class fMainWindow : Form
    {
        private readonly Renderer _renderer = new();
        private readonly DebugRenderer _debug = new();

        private GameObjectFactory _factory = null!;
        private PlayerSprites _p1Sprites = null!;
        private PlayerSprites _p2Sprites = null!;
        private ScrollingBackground _scrollingBg = null!;
        private GameLogic _logic = null!;

        private readonly HudRenderer _hud = new();
        private readonly RaceFinishedOverlay _finishUI = new();

        private GamePhase _phase = GamePhase.MainMenu;

        private DateTime _lastTime;
        private bool _isReady;

        private const float SplitBgTexVMin = 0.02f;
        private const float SplitBgTexVMax = 0.96f;

        public fMainWindow()
        {
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = true;
            MinimizeBox = true;
            ClientSize = new Size(1280, 720);

            Controls.Add(glControl);

            _updateTimer.Interval = 15;
            _updateTimer.Tick += OnTimerTick;

            glControl.KeyPress += (s, e) => e.Handled = true;
            KeyPreview = true;
        }


        private void glControl_Load(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            GL.ClearColor(Color.MidnightBlue);

            _renderer.Initialize();
            _debug.Initialize();
            _renderer.SetScreenSize(glControl.Width, glControl.Height);

            string content = Path.Combine(AppContext.BaseDirectory, "Content");
            _factory = new GameObjectFactory(content);

            BonusRegistry.Load(content);

            _scrollingBg = new ScrollingBackground(
                Path.Combine(content, "Images", "Backgrounds"));

            string p1Dir = Path.Combine(content, "Images", "Players", "Player1");
            string p2Dir = Path.Combine(content, "Images", "Players", "Player2");
            _p1Sprites = new PlayerSprites(p1Dir, "p1");
            _p2Sprites = new PlayerSprites(p2Dir, "p2");

            InitGame();

            this.Activate();
            glControl.Focus();
            ActiveControl = glControl;

            BeginInvoke(() =>
            {
                _isReady = true;
                _updateTimer.Start();
            });
        }

        private void InitGame()
        {
            float scale = Math.Min(_renderer.ScaleX, _renderer.ScaleY);
            float pw = GameLogic.PlayerBaseWidth * scale;
            float ph = GameLogic.PlayerBaseHeight * scale;
            int W = glControl.Width;
            int H = glControl.Height;

            var p1 = _factory.CreatePlayer(
                Path.Combine("Images", "Players", "Player1", "p1_stand.png"),
                W * GameLogic.PlayerScreenAnchorX, H * GameLogic.LaneTopY, pw, ph);
            p1.Animator = new PlayerAnimator(_p1Sprites) { AnimationFPS = GameLogic.AnimationFPS };

            var p2 = _factory.CreatePlayer(
                Path.Combine("Images", "Players", "Player2", "p2_stand.png"),
                W * GameLogic.PlayerScreenAnchorX, H * GameLogic.LaneBottomY, pw, ph);
            p2.Animator = new PlayerAnimator(_p2Sprites) { AnimationFPS = GameLogic.AnimationFPS };

            var w1 = _factory.CreateWarning(0, 0, 40f);
            var w2 = _factory.CreateWarning(0, 0, 40f);

            _logic = new GameLogic(_factory);
            _logic.Init(p1, p2, w1, w2);
        }


        private void glControl_Resize(object sender, EventArgs e)
        {
            if (glControl.ClientRectangle.Width <= 0 ||
                glControl.ClientRectangle.Height <= 0) return;

            glControl.MakeCurrent();
            _renderer.SetScreenSize(glControl.Width, glControl.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState != FormWindowState.Maximized) return;

            var screen = Screen.FromControl(this);
            int w = screen.WorkingArea.Width;
            int h = screen.WorkingArea.Height;
            float targetAspect = 16f / 9f;
            float screenAspect = (float)w / h;

            if (screenAspect > targetAspect)
            {
                h = screen.WorkingArea.Height;
                w = (int)(h * targetAspect);
            }
            else
            {
                w = screen.WorkingArea.Width;
                h = (int)(w / targetAspect);
            }
            ClientSize = new Size(w, h);
        }


        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!_isReady || !glControl.Created) return;

            float dt = (float)(DateTime.Now - _lastTime).TotalSeconds;
            _lastTime = DateTime.Now;
            dt = Math.Min(dt, 0.1f);

            if (_phase == GamePhase.Playing)
            {
                _logic.Update(dt, glControl.Width);

                if (_logic.RaceFinished)
                    _phase = GamePhase.RaceFinished;
            }

            if (_phase == GamePhase.RaceFinished && IsKeyDown(Keys.Enter))
            {
                ReturnToMenu();
                return;
            }

            _debug.Update(dt,
                _logic.State1.Obstacles.Count + _logic.State2.Obstacles.Count + 2, 0);

            glControl.Invalidate();
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool IsKeyDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;


        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            if (!_isReady || _logic == null) return;

            glControl.MakeCurrent();

            int W = glControl.Width, H = glControl.Height;
            if (W <= 0 || H <= 0) return;

            GL.Viewport(0, 0, W, H);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            float slideW = _scrollingBg.GetSlideWidth(W, H);
            bool split = MathF.Abs(
                _logic.State1.Player.WorldX - _logic.State2.Player.WorldX) >= slideW * GameLogic.SplitThreshold;

            if (split) RenderSplitScreen(W, H);
            else RenderSingleScreen(W, H);

            GL.Viewport(0, 0, W, H);
            if (split) _renderer.DrawSplitScreenDivider(W, H, 4f);

            var dbgObjects = new List<GameObject>(
                _logic.State1.Obstacles.Cast<GameObject>()
                    .Concat(_logic.State2.Obstacles)
                    .Append(_logic.State1.Player)
                    .Append(_logic.State2.Player));
            _debug.Render(W, H, dbgObjects);

            DrawHud(W, H);

            glControl.SwapBuffers();
        }

        private void DrawHud(int W, int H)
        {
            if (_phase == GamePhase.Playing || _phase == GamePhase.RaceFinished)
            {
                _hud.Draw(_debug, W, H,
                    _logic.State1.Distance, _logic.State1.Score,
                    _logic.State2.Distance, _logic.State2.Score,
                    GameLogic.RaceGoalDistance);
            }

            if (_logic.CountingDown)
            {
                int secs = (int)Math.Ceiling(_logic.CountdownTime);
                string txt = secs > 0 ? secs.ToString() : "GO!";
                int cx = W / 2 - 20;
                int cy = H / 2 - 30;
                _debug.DrawText(txt, cx + 2, cy + 2, Color.FromArgb(180, 0, 0, 0), W, H);
                _debug.DrawText(txt, cx, cy, Color.White, W, H);
            }

            if (_phase == GamePhase.RaceFinished)
            {
                _finishUI.Draw(_debug, W, H,
                    _logic.State1.Distance, _logic.State1.Score,
                    _logic.State2.Distance, _logic.State2.Score);
            }
        }

        private void RenderSingleScreen(int W, int H)
        {
            var s1 = _logic.State1;
            var s2 = _logic.State2;

            float avgWorldX = (s1.Player.WorldX + s2.Player.WorldX) * 0.5f;
            float camSingle = avgWorldX - W * 0.5f;
            float playerScrX = W * GameLogic.PlayerScreenAnchorX;
            float yTop = H * GameLogic.LaneTopY;
            float yBot = H * GameLogic.LaneBottomY;

            var (bga, bgb) = _scrollingBg.GetSlides(0, 0, W, H, camSingle);
            _renderer.Render(new List<GameObject> { bga, bgb });

            // screenX = anchor + offset from average camera position
            float sx1 = playerScrX + (s1.Player.WorldX - avgWorldX);
            float sx2 = playerScrX + (s2.Player.WorldX - avgWorldX);

            s1.Player.Position = new Vector2(sx1, yTop + s1.Player.JumpYOffset);
            s2.Player.Position = new Vector2(sx2, yBot + s2.Player.JumpYOffset);

            SetObstaclePositions(s1, yTop, W);
            SetObstaclePositions(s2, yBot, W);

            _renderer.Render(new List<GameObject> { s1.Player, s2.Player });
            RenderObstacles(s1.Obstacles);
            RenderObstacles(s2.Obstacles);
            RenderBonusPickups(s1, yTop + GameLogic.BonusLaneYOffset, W, 0, 0, W, H);
            RenderBonusPickups(s2, yBot + GameLogic.BonusLaneYOffset, W, 0, 0, W, H);
            RenderBonusEffects(s1, s1.Player.Position, 0, 0, W, H);
            RenderBonusEffects(s2, s2.Player.Position, 0, 0, W, H);

            SetWarningPosition(s1.Warning, sx1, yTop, W);
            SetWarningPosition(s2.Warning, sx2, yBot, W);
            RenderWarnings(0, 0, W, H);
        }

        private void RenderSplitScreen(int W, int H)
        {
            var s1 = _logic.State1;
            var s2 = _logic.State2;
            int h2 = H / 2;
            float pScrX = W * GameLogic.PlayerScreenAnchorX;
            float p1Y = h2 * GameLogic.SplitLaneTopY + h2;
            float p2Y = h2 * GameLogic.SplitLaneBottomY;

            // Top half – player 1
            float cam1 = s1.Player.WorldX - W * GameLogic.PlayerScreenAnchorX;
            s1.Player.Position = new Vector2(pScrX, p1Y + s1.Player.JumpYOffset);
            var (bg1a, bg1b) = _scrollingBg.GetSlides(0, h2, W, h2, cam1, SplitBgTexVMin, SplitBgTexVMax);
            _renderer.Render(new List<GameObject> { bg1a, bg1b }, 0, h2, W, h2);
            SetObstaclePositions(s1, p1Y, W);
            _renderer.Render(new List<GameObject> { s1.Player }, 0, h2, W, h2);
            RenderObstacles(s1.Obstacles, 0, h2, W, h2);
            RenderBonusPickups(s1, p1Y + GameLogic.BonusLaneYOffset, W, 0, h2, W, h2);
            RenderBonusEffects(s1, s1.Player.Position, 0, h2, W, h2);

            // Bottom half – player 2
            float cam2 = s2.Player.WorldX - W * GameLogic.PlayerScreenAnchorX;
            s2.Player.Position = new Vector2(pScrX, p2Y + s2.Player.JumpYOffset);
            var (bg2a, bg2b) = _scrollingBg.GetSlides(0, 0, W, h2, cam2, SplitBgTexVMin, SplitBgTexVMax);
            _renderer.Render(new List<GameObject> { bg2a, bg2b }, 0, 0, W, h2);
            SetObstaclePositions(s2, p2Y, W);
            _renderer.Render(new List<GameObject> { s2.Player }, 0, 0, W, h2);
            RenderObstacles(s2.Obstacles, 0, 0, W, h2);
            RenderBonusPickups(s2, p2Y + GameLogic.BonusLaneYOffset, W, 0, 0, W, h2);
            RenderBonusEffects(s2, s2.Player.Position, 0, 0, W, h2);

            SetWarningPosition(s1.Warning, pScrX, p1Y, W);
            SetWarningPosition(s2.Warning, pScrX, p2Y, W);
            RenderWarnings(0, h2, W, h2);
        }


        private void SetObstaclePositions(PlayerState state, float playerScreenY, float screenWidth)
        {
            float pScrX = screenWidth * GameLogic.PlayerScreenAnchorX;
            foreach (var obs in state.Obstacles)
                obs.Position = new Vector2(
                    pScrX + (obs.WorldX - state.Player.WorldX),
                    playerScreenY);
        }

        private static void SetWarningPosition(
            WarningEffect warning, float playerScreenX, float playerScreenY, float screenWidth)
        {
            if (!warning.IsActive) return;
            warning.Position = new Vector2(screenWidth - 60f, playerScreenY);
        }

        private void RenderObstacles(
            List<Obstacle> obstacles,
            int vx = 0, int vy = 0, int vw = 0, int vh = 0)
        {
            var list = obstacles.Cast<GameObject>().ToList();
            if (vw > 0 && vh > 0)
                _renderer.Render(list, vx, vy, vw, vh);
            else
                _renderer.Render(list);
        }

        private void RenderWarnings(int vx, int vy, int vw, int vh)
        {
            var list = new List<GameObject>();
            if (_logic.State1.Warning.IsActive) list.Add(_logic.State1.Warning);
            if (_logic.State2.Warning.IsActive) list.Add(_logic.State2.Warning);
            if (list.Count > 0)
                _renderer.Render(list, vx, vy, vw, vh);
        }

        private void RenderBonusPickups(
            PlayerState state, float screenY, float screenWidth,
            int vx, int vy, int vw, int vh)
        {
            float pScrX = screenWidth * GameLogic.PlayerScreenAnchorX;
            var list = new List<GameObject>();

            foreach (var pickup in state.BonusPickups)
            {
                if (pickup.IsCollected) continue;
                float sx = pScrX + (pickup.WorldX - state.Player.WorldX);
                pickup.Position = new Vector2(sx, screenY + pickup.BobOffset);
                list.Add(pickup);
            }

            if (list.Count > 0)
                _renderer.Render(list, vx, vy, vw, vh);
        }

        private void RenderBonusEffects(
            PlayerState state, Vector2 playerScreenPos,
            int vx, int vy, int vw, int vh)
        {
            var list = new List<GameObject>();

            foreach (var bonus in state.ActiveBonuses)
            {
                if (bonus.EffectSprite == null) continue;

                float ex = playerScreenPos.X - state.Player.Width * 0.6f;
                float ey = playerScreenPos.Y;

                var effectObj = new GameObject(bonus.EffectSprite)
                {
                    Position = new Vector2(ex, ey),
                    Width = state.Player.Width * 0.7f,
                    Height = state.Player.Height * 0.9f,
                    ScaleMode = ScaleMode.Stretch
                };
                list.Add(effectObj);
            }

            if (list.Count > 0)
                _renderer.Render(list, vx, vy, vw, vh);
        }


        public void StartGame()
        {
            _phase = GamePhase.Playing;
            _lastTime = DateTime.Now;
            Show();
            Activate();
            glControl.Focus();
        }

        private void ReturnToMenu()
        {
            _phase = GamePhase.MainMenu;
            InitGame();
            Hide();

            Program.ShowMainMenu();
        }


        private bool _gameDisposed;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_gameDisposed) { base.OnFormClosed(e); return; }
            _gameDisposed = true;

            _updateTimer.Stop();
            _isReady = false;

            _renderer.Dispose();
            _debug.Dispose();
            _scrollingBg?.Dispose();
            _factory?.Dispose();
            _p1Sprites?.Dispose();
            _p2Sprites?.Dispose();
            base.OnFormClosed(e);
        }
    }

}