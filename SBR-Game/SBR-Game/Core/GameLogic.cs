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
        public const float RaceGoalDistance = 100_000f;
        public const float JumpDuration = 0.55f;

        private const float BonusMinSpawnDist = 4000f;
        private const float BonusMaxSpawnDist = 8000f;
        public const float BonusLaneYOffset = 130f;

        private static float SlowdownForLevel(int level) => 1f - 0.15f * level;

        public PlayerState State1 { get; private set; } = null!;
        public PlayerState State2 { get; private set; } = null!;
        public bool RaceFinished { get; private set; }
        public int WinnerSlot { get; private set; }
        public bool CountingDown => _startTimer > 0f;
        public float CountdownTime => _startTimer;

        private float _startTimer;
        private bool _w_wasUp = true;
        private bool _up_wasUp = true;

        private readonly GameObjectFactory _factory;

        public GameLogic(GameObjectFactory factory) => _factory = factory;


        public void Init(Player player1, Player player2, WarningEffect warning1, WarningEffect warning2)
        {
            State1 = new PlayerState(player1, warning1, ObstacleMinSpawnDist);
            State2 = new PlayerState(player2, warning2, ObstacleMinSpawnDist);
            RaceFinished = false;
            _startTimer = StartDelay;
            _w_wasUp = true;
            _up_wasUp = true;
            player1.Speed = 0f;
            player2.Speed = 0f;
        }


        public void Update(float dt, int screenWidth)
        {
            if (RaceFinished) return;

            if (_startTimer > 0f)
            {
                _startTimer = Math.Max(0f, _startTimer - dt);
                return;
            }

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
            CheckRaceFinished();
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


        private void UpdateObstacles(PlayerState state, float dt, int screenWidth)
        {
            SpawnObstacleIfNeeded(state, screenWidth);
            DespawnPassedObstacles(state, screenWidth);
        }

        private void SpawnObstacleIfNeeded(PlayerState state, int screenWidth)
        {
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
            float scale = 0.8f + Random.Shared.NextSingle() * 0.4f;

            var obs = _factory.CreateBush(level, worldX, 0,
                PlayerBaseWidth * scale,
                PlayerBaseHeight * scale * 0.6f);

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
                var b = state.BonusPickups[i];
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

            var def = BonusRegistry.All[Random.Shared.Next(BonusRegistry.All.Count)];
            var pickup = _factory.CreateBonusPickup(def, worldX, 200f);
            state.BonusPickups.Add(pickup);
        }

        private static void CheckBonusPickups(PlayerState state)
        {
            if (!state.Player.IsJumping) return;   // must be airborne to collect

            float pX = state.Player.WorldX;
            float pJumpOffset = state.Player.JumpYOffset;

            foreach (var pickup in state.BonusPickups)
            {
                if (pickup.IsCollected) continue;

                // X overlap (generous hitbox for pickups)
                float dx = Math.Abs(pickup.WorldX - pX);
                if (dx > pickup.Width * 0.8f) continue;

                // Player must be high enough in their arc
                if (pJumpOffset < Player.JumpPeakPixels * 0.3f) continue;

                pickup.Collect();
                state.ActiveBonuses.Add(
                    new BonusModifier(pickup.Definition, pickup.EffectSprite));
            }
        }


        private static void CheckCollisions(PlayerState state)
        {
            if (state.Player.IsInvincible) return;
            if (state.HasBonusInvincibility) return;

            float pLeft = state.Player.WorldX - state.Player.Width * 0.20f;
            float pRight = state.Player.WorldX + state.Player.Width * 0.20f;

            foreach (var obs in state.Obstacles)
            {
                if (obs.WorldX < pLeft - obs.Width * 0.5f) continue;
                if (obs.WorldX > pRight + obs.Width * 0.5f) continue;

                float overlap = pRight - (obs.WorldX - obs.Width * 0.5f);
                if (overlap <= 0f) continue;

                state.Player.ApplySlowdown(SlowdownForLevel(obs.Level), MinSpeed);
                state.Player.TriggerHitFlash();
                break;
            }
        }


        private static void UpdateWarning(PlayerState state, float dt)
        {
            Obstacle? closest = null;
            float closestDist = float.MaxValue;

            foreach (var obs in state.Obstacles)
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


        private void CheckRaceFinished()
        {
            if (!State1.HasFinished && State1.Player.WorldX >= RaceGoalDistance)
                State1.FinalDistance = State1.Player.WorldX;
            if (!State2.HasFinished && State2.Player.WorldX >= RaceGoalDistance)
                State2.FinalDistance = State2.Player.WorldX;

            if (!State1.HasFinished && !State2.HasFinished) return;

            RaceFinished = true;
            if (State1.HasFinished && State2.HasFinished)
                WinnerSlot = State1.FinalDistance >= State2.FinalDistance ? 1 : 2;
            else
                WinnerSlot = State1.HasFinished ? 1 : 2;
        }
    }
}