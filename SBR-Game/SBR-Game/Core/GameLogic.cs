
using SBR_Game.Core;
using SBR_Game.Gameplay;
using SBR_Game.Gameplay.Bonuses;
using SBR_Game.Rendering;
using System.Runtime.InteropServices;

namespace SBR_Game
{
    public class GameLogic
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public const float AnimationFPS = 12f;
        public const float MinSpeed = 0f;
        public const float MaxSpeed = 1200f;
        public const float Acceleration = 120f;
        public const float SplitThreshold = 0.35f;
        public const float ObstacleMinSpawnDist = 3500f;
        public const float ObstacleMaxSpawnDist = 5500f;
        public const float ObstacleDespawnOffset = 200f;
        public const float WarningDistance = 2000f;
        public const float WarningDuration = 0.5f;
        public const float PlayerBaseWidth = 200f;
        public const float PlayerBaseHeight = 250f;
        public const float PlayerScreenAnchorX = 0.15f;
        public const float LaneTopY = 0.53f;
        public const float LaneBottomY = 0.15f;
        public const float SplitLaneTopY = 0.25f;
        public const float SplitLaneBottomY = 0.25f;
        public const float StartDelay = 3f;
        public const float RaceGoalDistance = 200_000f;
        public const float FinishMusicTriggerDistance = 199_000f;
        public const float JumpDuration = 0.55f;

        public const float LaggardTeleportDistance = 3_000f;

        private const float BonusMinSpawnDist = 4000f;
        private const float BonusMaxSpawnDist = 8000f;
        public const float BonusLaneYOffset = 130f;

        private static float SlowdownForLevel(int level) => 1f - 0.15f * level;

        public PlayerState State1 { get; private set; } = null!;
        public PlayerState State2 { get; private set; } = null!;

        public bool FirstFinished { get; private set; }
        public int WinnerSlot { get; private set; }
        public bool BothOffScreen { get; private set; }

        public bool CountingDown => _startTimer > 0f;
        public float CountdownTime => _startTimer;
        public bool ShouldPlayFinishMusic => _shouldPlayFinishMusic;

        public float FrozenCameraWorldX { get; private set; }

        private float _startTimer;
        private bool _w_wasUp = true;
        private bool _up_wasUp = true;
        private bool _shouldPlayFinishMusic = false;
        private int _lastScreenWidth = 1280;

        private bool _p1OffScreen = false;
        private bool _p2OffScreen = false;
        private bool _laggardTeleported = false;

        private readonly GameObjectFactory _factory;
        public Action? OnFinishMusicTriggered { get; set; }

        private static GameLogic _instance = null!;
        public static GameLogic Instance => _instance;

        public GameLogic(GameObjectFactory factory)
        {
            _instance = this;
            _factory = factory;
        }

        public void Init(Player player1, Player player2, WarningEffect warning1, WarningEffect warning2)
        {
            State1 = new PlayerState(player1, warning1, ObstacleMinSpawnDist);
            State2 = new PlayerState(player2, warning2, ObstacleMinSpawnDist);
            FirstFinished = false;
            BothOffScreen = false;
            WinnerSlot = 0;
            FrozenCameraWorldX = 0f;
            _p1OffScreen = false;
            _p2OffScreen = false;
            _laggardTeleported = false;
            _shouldPlayFinishMusic = false;
            _startTimer = StartDelay;
            _w_wasUp = true;
            _up_wasUp = true;
            player1.Speed = 0f;
            player2.Speed = 0f;
        }


        public void Update(float dt, int screenWidth)
        {
            if (BothOffScreen) return;
            _lastScreenWidth = screenWidth;

            if (_startTimer > 0f)
            {
                _startTimer = Math.Max(0f, _startTimer - dt);
                return;
            }

            if (!FirstFinished)
            {
                // Обычная игра с управлением
                HandleInput(dt);
                UpdatePlayer(State1, dt);
                UpdatePlayer(State2, dt);
                UpdateObstacles(State1, dt, screenWidth);
                UpdateObstacles(State2, dt, screenWidth);
                UpdateBonuses(State1, dt, screenWidth);
                UpdateBonuses(State2, dt, screenWidth);
                CheckCollisions(State1);
                CheckCollisions(State2);
                CheckBonusPickups(State1);
                CheckBonusPickups(State2);
                UpdateWarning(State1, dt);
                UpdateWarning(State2, dt);
                CheckFinishMusicTrigger();
                CheckFirstFinish(screenWidth);
            }
            else
            {
                // автопилот финала
                UpdateAutopilotPlayer(State1, dt);
                UpdateAutopilotPlayer(State2, dt);
                TryTeleportLaggard();
                State1.Player.UpdateAnimation(dt);
                State2.Player.UpdateAnimation(dt);
                CheckPlayersOffScreen(screenWidth);
            }

            if (State1.Player.Animator?.CurrentFrameChanged == true && !State1.Player.IsJumping && State1.Player.Speed > 0)
                OnHoofstepP1?.Invoke();
            if (State2.Player.Animator?.CurrentFrameChanged == true && !State2.Player.IsJumping && State2.Player.Speed > 0)
                OnHoofstepP2?.Invoke();
        }


        private void HandleInput(float dt)
        {
            bool wDown = IsDown(Keys.W);
            bool upDown = IsDown(Keys.Up);

            if (wDown && _w_wasUp) State1.Player.TryJump(JumpDuration);
            if (upDown && _up_wasUp) State2.Player.TryJump(JumpDuration);

            _w_wasUp = !wDown;
            _up_wasUp = !upDown;
        }

        private static bool IsDown(Keys key) => (GetAsyncKeyState((int)key) & 0x8000) != 0;


        private static void UpdatePlayer(PlayerState state, float dt)
        {
            if (state.HasFinished) return;

            float effAccel = state.EffectiveAcceleration(Acceleration);
            float effMaxSpeed = state.EffectiveMaxSpeed(MaxSpeed);

            state.Player.Accelerate(effAccel, effMaxSpeed, dt);
            state.Player.Advance(dt);
            state.Player.UpdateAnimation(dt);

            for (int i = state.ActiveBonuses.Count - 1; i >= 0; i--)
            {
                state.ActiveBonuses[i].Update(dt);
                if (state.ActiveBonuses[i].IsExpired)
                    state.ActiveBonuses.RemoveAt(i);
            }
        }

        private static void UpdateAutopilotPlayer(PlayerState state, float dt)
        {
            state.Player.Speed = MaxSpeed;
            state.Player.Advance(dt);
        }


        private void UpdateObstacles(PlayerState state, float dt, int screenWidth)
        {
            SpawnObstacleIfNeeded(state, screenWidth);
            DespawnPassedObstacles(state, screenWidth);
        }

        private void SpawnObstacleIfNeeded(PlayerState state, int screenWidth)
        {
            if (FirstFinished) return;

            bool hasAhead = state.Obstacles.Any(
                o => o.WorldX > state.Player.WorldX &&
                     ScreenX(o.WorldX, state, screenWidth) > -ObstacleDespawnOffset);
            if (hasAhead) return;

            float speedFactor = 1f + (state.Player.Speed / MaxSpeed);
            float dist = ObstacleMinSpawnDist * speedFactor
                       + Random.Shared.NextSingle()
                         * (ObstacleMaxSpawnDist - ObstacleMinSpawnDist) * speedFactor;

            float worldX = state.Player.WorldX + dist;
            int level = Random.Shared.Next(1, 4);
            int type = Random.Shared.Next(1, 4);

            Obstacle obs = type switch
            {
                1 => _factory.CreateBush(level, worldX, 0, PlayerBaseWidth, PlayerBaseHeight * 0.6f),
                2 => _factory.CreateBarrier(level, worldX, 0, PlayerBaseWidth, PlayerBaseHeight * 0.7f, -25),
                3 => _factory.CreateLake(level, worldX, 0, 300f, 320f, -65),
                _ => _factory.CreateBush(level, worldX, 0, PlayerBaseWidth, PlayerBaseHeight * 0.6f)
            };

            state.Obstacles.Add(obs);
            state.NextObstacleWorldX = worldX;
        }

        private static void DespawnPassedObstacles(PlayerState state, int screenWidth)
        {
            for (int i = state.Obstacles.Count - 1; i >= 0; i--)
                if (ScreenX(state.Obstacles[i].WorldX, state, screenWidth) < -ObstacleDespawnOffset)
                    state.Obstacles.RemoveAt(i);
        }


        private void UpdateBonuses(PlayerState state, float dt, int screenWidth)
        {
            SpawnBonusIfNeeded(state, screenWidth);

            for (int i = state.BonusPickups.Count - 1; i >= 0; i--)
            {
                BonusPickup b = state.BonusPickups[i];
                if (b.IsCollected ||
                    ScreenX(b.WorldX, state, screenWidth) < -ObstacleDespawnOffset)
                {
                    state.BonusPickups.RemoveAt(i);
                    continue;
                }
                b.Update(dt);
            }
        }

        private void SpawnBonusIfNeeded(PlayerState state, int screenWidth)
        {
            if (BonusRegistry.All.Count == 0) return;

            bool hasAhead = state.BonusPickups.Any(
                b => !b.IsCollected &&
                     b.WorldX > state.Player.WorldX &&
                     ScreenX(b.WorldX, state, screenWidth) > -ObstacleDespawnOffset);
            if (hasAhead) return;

            float dist = BonusMinSpawnDist
                         + Random.Shared.NextSingle() * (BonusMaxSpawnDist - BonusMinSpawnDist);
            float worldX = state.Player.WorldX + dist;

            BonusDefinition def = BonusRegistry.All[Random.Shared.Next(BonusRegistry.All.Count)];
            BonusPickup pickup = _factory.CreateBonusPickup(def, worldX, 200f);
            state.BonusPickups.Add(pickup);
        }

        private static void CheckBonusPickups(PlayerState state)
        {
            if (!state.Player.IsJumping) return;

            float pX = state.Player.WorldX;
            float pJumpOffset = state.Player.JumpYOffset;

            foreach (BonusPickup pickup in state.BonusPickups)
            {
                if (pickup.IsCollected) continue;

                float dx = Math.Abs(pickup.WorldX - pX);
                if (dx > pickup.Width * 0.8f) continue;

                if (pJumpOffset < Player.JumpPeakPixels * 0.3f) continue;

                pickup.Collect();
                state.ActiveBonuses.Add(
                    new BonusModifier(pickup.Definition, pickup.EffectSprite));

                Instance.OnBonusPickup?.Invoke();
            }
        }


        private void CheckCollisions(PlayerState state)
        {
            if (state.Player.IsInvincible) return;
            if (state.HasBonusInvincibility) return;

            float pLeft = state.Player.WorldX - state.Player.Width * 0.20f;
            float pRight = state.Player.WorldX + state.Player.Width * 0.20f;

            foreach (Obstacle obs in state.Obstacles)
            {
                if (obs.WorldX < pLeft - obs.Width * 0.5f) continue;
                if (obs.WorldX > pRight + obs.Width * 0.5f) continue;

                float overlap = pRight - (obs.WorldX - obs.Width * 0.5f);
                if (overlap <= 0f) continue;

                state.OnObstacleHit?.Invoke();
                state.Player.ApplySlowdown(SlowdownForLevel(obs.Level), MinSpeed);
                state.Player.TriggerHitFlash();
                break;
            }
        }

        public Action? OnBonusPickup { get; set; }
        public Action? OnHoofstepP1 { get; set; }
        public Action? OnHoofstepP2 { get; set; }


        private static void UpdateWarning(PlayerState state, float dt)
        {
            Obstacle? closest = null;
            float closestDist = float.MaxValue;

            foreach (Obstacle obs in state.Obstacles)
            {
                float d = obs.WorldX - state.Player.WorldX;
                if (d > 0 && d < closestDist) { closestDist = d; closest = obs; }
            }

            if (closest == null)
            {
                if (state.Warning.IsActive) { state.Warning.Deactivate(); state.LastWarnedObstacle = null; }
                return;
            }

            if (!state.Warning.IsActive
                && closestDist <= WarningDistance
                && closest != state.LastWarnedObstacle)
            {
                state.Warning.Activate(WarningDuration);
                state.LastWarnedObstacle = closest;
            }

            if (state.Warning.IsActive && closestDist <= 0)
            {
                state.Warning.Deactivate();
                state.LastWarnedObstacle = null;
            }

            if (state.Warning.IsActive) state.Warning.Update(dt);
        }


        private static float ScreenX(float worldX, PlayerState state, int screenWidth)
        {
            return screenWidth * PlayerScreenAnchorX + (worldX - state.Player.WorldX);
        }

        public float FrozenScreenX(float worldX, int screenWidth)
        {
            return screenWidth * PlayerScreenAnchorX + (worldX - FrozenCameraWorldX);
        }


        private void CheckFinishMusicTrigger()
        {
            if (_shouldPlayFinishMusic) return;

            if (State1.Player.WorldX >= FinishMusicTriggerDistance ||
                State2.Player.WorldX >= FinishMusicTriggerDistance)
            {
                _shouldPlayFinishMusic = true;
                OnFinishMusicTriggered?.Invoke();

                FrozenCameraWorldX = RaceGoalDistance - _lastScreenWidth * PlayerScreenAnchorX;
            }
        }


        private void CheckFirstFinish(int screenWidth)
        {
            bool p1CrossedLine = State1.Player.WorldX >= RaceGoalDistance;
            bool p2CrossedLine = State2.Player.WorldX >= RaceGoalDistance;

            if (!p1CrossedLine && !p2CrossedLine) return;

            if (!FirstFinished)
            {
                FirstFinished = true;

                if (p1CrossedLine && p2CrossedLine)
                    WinnerSlot = State1.Player.WorldX >= State2.Player.WorldX ? 1 : 2;
                else
                    WinnerSlot = p1CrossedLine ? 1 : 2;

            }
        }

        private void TryTeleportLaggard()
        {
            if (_laggardTeleported) return;

            PlayerState laggard = WinnerSlot == 1 ? State2 : State1;
            float distToFinish = RaceGoalDistance - laggard.Player.WorldX;

            if (distToFinish > LaggardTeleportDistance)
            {
                laggard.Player.WorldX = RaceGoalDistance - LaggardTeleportDistance;
                _laggardTeleported = true;
            }
        }

        private void CheckPlayersOffScreen(int screenWidth)
        {
            float p1ScrX = FrozenScreenX(State1.Player.WorldX, screenWidth);
            float p2ScrX = FrozenScreenX(State2.Player.WorldX, screenWidth);

            if (!_p1OffScreen && p1ScrX > screenWidth + 150f)
                _p1OffScreen = true;

            if (!_p2OffScreen && p2ScrX > screenWidth + 150f)
                _p2OffScreen = true;

            if (_p1OffScreen && _p2OffScreen)
                BothOffScreen = true;
        }
    }
}
