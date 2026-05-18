using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SBR_Game.Audio;
using SBR_Game.Core;
using SBR_Game.Gameplay;
using SBR_Game.Gameplay.Bonuses;
using SBR_Game.Rendering;
using SBR_Game.Rendering.UI;
using SBR_Game.States;
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
        private AudioManager _audioManager = null!;

        private readonly HudRenderer _hud = new();
        private readonly RaceFinishedOverlay _finishUI = new();

        private GameObject? _finishSprite;

        private const float FinishSpriteHeight = 700f;
        private const float FinishSpriteWidth = 500f;

        private GamePhase _phase = GamePhase.MainMenu;

        private DateTime _lastTime;
        private bool _isReady;
        private float _dt;

        private const float SplitBgTexVMin = 0.02f;
        private const float SplitBgTexVMax = 0.96f;

        public fMainWindow()
        {
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;

            Screen screen = Screen.FromPoint(Cursor.Position);
            Bounds = screen.Bounds;

            ClientSize = screen.Bounds.Size; 

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
            _hud.Initialize();
            _finishUI.Initialize();
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

            _audioManager = new AudioManager();
            _audioManager.Initialize(content);
            _audioManager.PlayGameMusic();

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

            Player p1 = _factory.CreatePlayer(
                Path.Combine("Images", "Players", "Player1", "p1_stand.png"),
                W * GameLogic.PlayerScreenAnchorX, H * GameLogic.LaneTopY, pw, ph);
            p1.Animator = new PlayerAnimator(_p1Sprites);

            Player p2 = _factory.CreatePlayer(
                Path.Combine("Images", "Players", "Player2", "p2_stand.png"),
                W * GameLogic.PlayerScreenAnchorX, H * GameLogic.LaneBottomY, pw, ph);
            p2.Animator = new PlayerAnimator(_p2Sprites);

            WarningEffect w1 = _factory.CreateWarning(0, 0, 70f);
            WarningEffect w2 = _factory.CreateWarning(0, 0, 70f);

            _logic = new GameLogic(_factory);
            _logic.Init(p1, p2, w1, w2);

            if (_audioManager != null)
            {
                _logic.OnHoofstepP1 += _audioManager.PlayHoofstepRandomPitch;
                _logic.OnHoofstepP2 += _audioManager.PlayHoofstepRandomPitch;
                _logic.OnBonusPickup += _audioManager.PlayBonusPickup;
                _logic.OnFinishMusicTriggered += _audioManager.PlayFinishMusic;
                _logic.State1.OnObstacleHit += _audioManager.PlayObstacleHitPlayer1;
                _logic.State2.OnObstacleHit += _audioManager.PlayObstacleHitPlayer2;
            }

            _finishSprite = _factory.CreateFinishSprite(
                GameLogic.RaceGoalDistance,
                H * GameLogic.LaneTopY,
                FinishSpriteWidth, FinishSpriteHeight);

            _finishUI.Reset();
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

        }


        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!_isReady || !glControl.Created) return;

            _dt = (float)(DateTime.Now - _lastTime).TotalSeconds;
            _lastTime = DateTime.Now;
            _dt = Math.Min(_dt, 0.1f);

            if (IsKeyDown(Keys.Escape) && (_phase == GamePhase.Playing || _phase == GamePhase.PostFinish || _phase == GamePhase.RaceFinished))
            {
                ReturnToMenu();
                return;
            }

            if (_phase == GamePhase.Playing)
            {
                _logic.Update(_dt, glControl.Width);


                if (_logic.FirstFinished && _phase == GamePhase.Playing)
                    _phase = GamePhase.PostFinish;
            }
            else if (_phase == GamePhase.PostFinish)
            {
                _logic.Update(_dt, glControl.Width);


                if (_logic.BothOffScreen)
                    _phase = GamePhase.RaceFinished;
            }

            if (_phase == GamePhase.RaceFinished && IsKeyDown(Keys.Enter)
                && _finishUI.IsFullyShown)
            {
                ReturnToMenu();
                return;
            }

            _debug.Update(_dt,
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

            if (_phase == GamePhase.PostFinish || _phase == GamePhase.RaceFinished)
            {

                RenderFrozenScreen(W, H);
            }
            else
            {

                float slideW = _scrollingBg.GetSlideWidth(W, H);
                bool split = MathF.Abs(
                    _logic.State1.Player.WorldX - _logic.State2.Player.WorldX) >= slideW * GameLogic.SplitThreshold;

                if (split) RenderSplitScreen(W, H);
                else RenderSingleScreen(W, H);

                GL.Viewport(0, 0, W, H);
                if (split) _renderer.DrawSplitScreenDivider(W, H, 4f);
            }

            GL.Viewport(0, 0, W, H);
            List<GameObject> dbgObjects = new List<GameObject>(
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

            if (_phase == GamePhase.Playing)
            {
                _hud.Draw(W, H,
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
                _finishUI.Draw(W, H,
                    _logic.State1.Distance, _logic.State1.Score,
                    _logic.State2.Distance, _logic.State2.Score,
                    _logic.WinnerSlot,
                    _dt);
            }
        }



        private void RenderFrozenScreen(int W, int H)
        {
            float frozenCam = _logic.FrozenCameraWorldX;


            (GameObject bga, GameObject bgb) = _scrollingBg.GetSlides(0, 0, W, H, frozenCam);
            _renderer.Render(new List<GameObject> { bga, bgb });

            float yTop = H * GameLogic.LaneTopY;
            float yBot = H * GameLogic.LaneBottomY;


            if (_finishSprite != null)
            {
                float finishScrX = _logic.FrozenScreenX(GameLogic.RaceGoalDistance, W);
                _finishSprite.Position = new Vector2(finishScrX, yTop + 150f);
                _finishSprite.Width = FinishSpriteWidth;
                _finishSprite.Height = FinishSpriteHeight;
                _renderer.Render(new List<GameObject> { _finishSprite });
            }


            float pScrX1 = _logic.FrozenScreenX(_logic.State1.Player.WorldX, W);
            float pScrX2 = _logic.FrozenScreenX(_logic.State2.Player.WorldX, W);

            _logic.State1.Player.Position = new Vector2(pScrX1, yTop + _logic.State1.Player.JumpYOffset);
            _logic.State2.Player.Position = new Vector2(pScrX2, yBot + _logic.State2.Player.JumpYOffset);


            SetObstaclePositionsFrozen(_logic.State1, yTop, frozenCam, W);
            SetObstaclePositionsFrozen(_logic.State2, yBot, frozenCam, W);


            List<GameObject> renderList = new List<GameObject>();
            if (pScrX1 <= W + 200f) renderList.Add(_logic.State1.Player);
            if (pScrX2 <= W + 200f) renderList.Add(_logic.State2.Player);
            _renderer.Render(renderList);

            RenderObstacles(_logic.State1.Obstacles);
            RenderObstacles(_logic.State2.Obstacles);
        }


        private void RenderSingleScreen(int W, int H)
        {
            PlayerState s1 = _logic.State1;
            PlayerState s2 = _logic.State2;

            float avgWorldX = (s1.Player.WorldX + s2.Player.WorldX) * 0.5f;
            float camSingle = avgWorldX - W * 0.5f;
            float playerScrX = W * GameLogic.PlayerScreenAnchorX;
            float yTop = H * GameLogic.LaneTopY;
            float yBot = H * GameLogic.LaneBottomY;

            (GameObject bga, GameObject bgb) = _scrollingBg.GetSlides(0, 0, W, H, camSingle);
            _renderer.Render(new List<GameObject> { bga, bgb });


            if (_finishSprite != null)
            {
                float finishScrX = W * GameLogic.PlayerScreenAnchorX + (GameLogic.RaceGoalDistance - avgWorldX);
                _finishSprite.Position = new Vector2(finishScrX, yTop + 150);
                _finishSprite.Width = FinishSpriteWidth;
                _finishSprite.Height = FinishSpriteHeight;

                if (finishScrX > -FinishSpriteWidth && finishScrX < W + FinishSpriteWidth)
                    _renderer.Render(new List<GameObject> { _finishSprite });
            }

            float sx1 = playerScrX + (s1.Player.WorldX - avgWorldX);
            float sx2 = playerScrX + (s2.Player.WorldX - avgWorldX);

            s1.Player.Position = new Vector2(sx1, yTop + s1.Player.JumpYOffset);
            s2.Player.Position = new Vector2(sx2, yBot + s2.Player.JumpYOffset);

            SetObstaclePositions(s1, yTop, avgWorldX, W);
            SetObstaclePositions(s2, yBot, avgWorldX, W);

            _renderer.Render(new List<GameObject> { s1.Player, s2.Player });
            RenderObstacles(s1.Obstacles);
            RenderObstacles(s2.Obstacles);
            RenderBonusPickups(s1, yTop + GameLogic.BonusLaneYOffset, avgWorldX, W, 0, 0, W, H);
            RenderBonusPickups(s2, yBot + GameLogic.BonusLaneYOffset, avgWorldX, W, 0, 0, W, H);
            RenderBonusEffects(s1, s1.Player.Position, 0, 0, W, H);
            RenderBonusEffects(s2, s2.Player.Position, 0, 0, W, H);

            SetWarningPosition(s1.Warning, sx1, yTop, W);
            SetWarningPosition(s2.Warning, sx2, yBot, W);
            RenderWarnings(0, 0, W, H);
        }

        private void RenderSplitScreen(int W, int H)
        {
            PlayerState s1 = _logic.State1;
            PlayerState s2 = _logic.State2;
            int h2 = H / 2;
            float pScrX = W * GameLogic.PlayerScreenAnchorX;
            float p1Y = h2 * GameLogic.SplitLaneTopY + h2;
            float p2Y = h2 * GameLogic.SplitLaneBottomY;


            float cam1 = s1.Player.WorldX - W * GameLogic.PlayerScreenAnchorX;
            s1.Player.Position = new Vector2(pScrX, p1Y + s1.Player.JumpYOffset);
            (GameObject bg1a, GameObject bg1b) = _scrollingBg.GetSlides(0, h2, W, h2, cam1, SplitBgTexVMin, SplitBgTexVMax);
            _renderer.Render(new List<GameObject> { bg1a, bg1b }, 0, h2, W, h2);


            if (_finishSprite != null)
            {
                float fsx1 = pScrX + (GameLogic.RaceGoalDistance - s1.Player.WorldX);
                if (fsx1 > -FinishSpriteWidth && fsx1 < W + FinishSpriteWidth)
                {
                    _finishSprite.Position = new Vector2(fsx1, p1Y + 280f);
                    _finishSprite.Width = FinishSpriteWidth;
                    _finishSprite.Height = FinishSpriteHeight * 0.6f;
                    _renderer.Render(new List<GameObject> { _finishSprite }, 0, h2, W, h2);
                }
            }

            SetObstaclePositions(s1, p1Y, s1.Player.WorldX, W);
            _renderer.Render(new List<GameObject> { s1.Player }, 0, h2, W, h2);
            RenderObstacles(s1.Obstacles, 0, h2, W, h2);
            RenderBonusPickups(s1, p1Y + GameLogic.BonusLaneYOffset, s1.Player.WorldX, W, 0, h2, W, h2);
            RenderBonusEffects(s1, s1.Player.Position, 0, h2, W, h2);


            float cam2 = s2.Player.WorldX - W * GameLogic.PlayerScreenAnchorX;
            s2.Player.Position = new Vector2(pScrX, p2Y + s2.Player.JumpYOffset);
            (GameObject bg2a, GameObject bg2b) = _scrollingBg.GetSlides(0, 0, W, h2, cam2, SplitBgTexVMin, SplitBgTexVMax);
            _renderer.Render(new List<GameObject> { bg2a, bg2b }, 0, 0, W, h2);


            if (_finishSprite != null)
            {
                float fsx2 = pScrX + (GameLogic.RaceGoalDistance - s2.Player.WorldX);
                if (fsx2 > -FinishSpriteWidth && fsx2 < W + FinishSpriteWidth)
                {
                    _finishSprite.Position = new Vector2(fsx2, p2Y + 280f);
                    _finishSprite.Width = FinishSpriteWidth;
                    _finishSprite.Height = FinishSpriteHeight * 0.6f;
                    _renderer.Render(new List<GameObject> { _finishSprite }, 0, 0, W, h2);
                }
            }

            SetObstaclePositions(s2, p2Y, s2.Player.WorldX, W);
            _renderer.Render(new List<GameObject> { s2.Player }, 0, 0, W, h2);
            RenderObstacles(s2.Obstacles, 0, 0, W, h2);
            RenderBonusPickups(s2, p2Y + GameLogic.BonusLaneYOffset, s2.Player.WorldX, W, 0, 0, W, h2);
            RenderBonusEffects(s2, s2.Player.Position, 0, 0, W, h2);

            SetWarningPosition(s1.Warning, pScrX, p1Y, W);
            SetWarningPosition(s2.Warning, pScrX, p2Y, W);
            RenderWarnings(0, h2, W, h2, _logic.State1.Warning);
            RenderWarnings(0, 0, W, h2, _logic.State2.Warning);
        }


        private void SetObstaclePositions(PlayerState state, float playerScreenY, float avgWorldX, float screenWidth)
        {
            float pScrX = screenWidth * GameLogic.PlayerScreenAnchorX;
            foreach (Obstacle obs in state.Obstacles)
                obs.Position = new Vector2(
                    pScrX + (obs.WorldX - avgWorldX),
                    playerScreenY + obs.WorldYOffset);
        }

        private void SetObstaclePositionsFrozen(PlayerState state, float playerScreenY, float frozenCam, float screenWidth)
        {

            float anchorX = screenWidth * GameLogic.PlayerScreenAnchorX;
            foreach (Obstacle obs in state.Obstacles)
                obs.Position = new Vector2(
                    anchorX + (obs.WorldX - frozenCam),
                    playerScreenY + obs.WorldYOffset);
        }

        private static void SetWarningPosition(WarningEffect warning, float playerScreenX, float playerScreenY, float screenWidth)
        {
            if (!warning.IsActive) return;
            warning.Position = new Vector2(screenWidth - 60f, playerScreenY);
        }

        private void RenderObstacles(List<Obstacle> obstacles, int vx = 0, int vy = 0, int vw = 0, int vh = 0)
        {
            List<GameObject> list = obstacles.Cast<GameObject>().ToList();
            if (vw > 0 && vh > 0)
                _renderer.Render(list, vx, vy, vw, vh);
            else
                _renderer.Render(list);
        }

        private void RenderWarnings(int vx, int vy, int vw, int vh, WarningEffect? specificWarning = null)
        {
            List<GameObject> list = new List<GameObject>();
            if (specificWarning != null)
            {
                if (specificWarning.IsActive) list.Add(specificWarning);
            }
            else
            {
                if (_logic.State1.Warning.IsActive) list.Add(_logic.State1.Warning);
                if (_logic.State2.Warning.IsActive) list.Add(_logic.State2.Warning);
            }
            if (list.Count > 0)
                _renderer.Render(list, vx, vy, vw, vh);
        }

        private void RenderBonusPickups(PlayerState state, float screenY, float avgWorldX, float screenWidth, int vx, int vy, int vw, int vh)
        {
            float pScrX = screenWidth * GameLogic.PlayerScreenAnchorX;
            List<GameObject> list = new List<GameObject>();

            foreach (BonusPickup pickup in state.BonusPickups)
            {
                if (pickup.IsCollected) continue;
                float sx = pScrX + (pickup.WorldX - avgWorldX);
                pickup.Position = new Vector2(sx, screenY + pickup.BobOffset);
                list.Add(pickup);
            }

            if (list.Count > 0)
                _renderer.Render(list, vx, vy, vw, vh);
        }

        private void RenderBonusEffects(PlayerState state, Vector2 playerScreenPos, int vx, int vy, int vw, int vh)
        {
            List<GameObject> list = new List<GameObject>();

            foreach (IPlayerModifier bonus in state.ActiveBonuses)
            {
                if (bonus.EffectSprite == null) continue;

                GameObject effectObj = new GameObject(bonus.EffectSprite)
                {
                    Position = new Vector2(playerScreenPos.X, playerScreenPos.Y),
                    Width = state.Player.Width,
                    Height = state.Player.Height,
                    ScaleMode = ScaleMode.KeepAspectRatio
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

            _audioManager?.PlayGameMusic();

            Show();

            PerformLayout();
            glControl.MakeCurrent();
            _renderer.SetScreenSize(glControl.Width, glControl.Height);

            BringToFront();
            Activate();
            glControl.Focus();
            glControl.Invalidate();
        }

        private void ReturnToMenu()
        {
            _phase = GamePhase.MainMenu;
            InitGame();

            _audioManager?.StopMusic();
            _audioManager?.PlayMainMenuMusic();

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
            _hud.Dispose();
            _finishUI.Dispose();
            _scrollingBg?.Dispose();
            _factory?.Dispose();
            _p1Sprites?.Dispose();
            _p2Sprites?.Dispose();
            _audioManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}