using SBR_Game.Rendering;

namespace SBR_Game.Gameplay
{
    public class PlayerSprites : IDisposable
    {
        public Texture2D Stand { get; }
        public Texture2D Run1 { get; }
        public Texture2D Run2 { get; }
        public Texture2D Run3 { get; }
        public Texture2D Jump { get; }

        public PlayerSprites(string folderPath, string prefix)
        {
            Stand = Load(folderPath, $"{prefix}_stand.png");
            Run1 = Load(folderPath, $"{prefix}_run_1.png");
            Run2 = Load(folderPath, $"{prefix}_run_2.png");
            Run3 = Load(folderPath, $"{prefix}_run_3.png");
            Jump = Load(folderPath, $"{prefix}_jump.png");
        }

        private static Texture2D Load(string folder, string file)
            => Texture2D.LoadFromFile(Path.Combine(folder, file));

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stand.Dispose(); Run1.Dispose(); Run2.Dispose();
            Run3.Dispose(); Jump.Dispose();
        }
    }


    public class PlayerAnimator
    {
        private readonly PlayerSprites _sprites;
        private float _animTimer;
        private int _currentFrame;

        public float AnimationFPS { get; set; } = 12f;
        public Texture2D CurrentTexture { get; private set; }

        public PlayerAnimator(PlayerSprites sprites)
        {
            _sprites = sprites;
            CurrentTexture = sprites.Stand;
        }

        public Texture2D GetStandSprite() => _sprites.Stand;

        public void Update(float deltaTime, float speed, bool jumping)
        {
            if (jumping)
            {
                CurrentTexture = _sprites.Jump;
                _animTimer = 0f;
                _currentFrame = 0;
                return;
            }

            if (speed <= 0)
            {
                CurrentTexture = _sprites.Stand;
                _animTimer = 0f;
                _currentFrame = 0;
                return;
            }

            _animTimer += deltaTime;
            float interval = 1f / AnimationFPS;
            if (_animTimer >= interval)
            {
                _animTimer = 0f;
                _currentFrame = (_currentFrame + 1) % 3;
                CurrentTexture = _currentFrame switch
                {
                    0 => _sprites.Run1,
                    1 => _sprites.Run2,
                    _ => _sprites.Run3
                };
            }
        }
    }


    public class Player : GameObject
    {
        public float WorldX { get; set; }
        public float Speed { get; set; }

        public bool IsJumping { get; private set; }
        public float JumpTimer { get; private set; }
        public float JumpDuration { get; private set; }

        public float JumpYOffset { get; private set; }

        private float _hitFlashTimer;
        private const float HitFlashDuration = 1.2f;
        private const float HitFlashInterval = 0.1f;
        public bool IsInvincible => IsJumping || _hitFlashTimer > 0f;

        public PlayerAnimator? Animator { get; set; }

        public Player(Texture2D texture) : base(texture)
        {
            ScaleMode = ScaleMode.KeepAspectRatio;
        }


        public void Accelerate(float acceleration, float maxSpeed, float dt)
            => Speed = Math.Min(maxSpeed, Speed + acceleration * dt);

        public void ApplySlowdown(float factor, float minSpeed)
            => Speed = Math.Max(minSpeed, Speed * factor);

        public void Advance(float dt)
            => WorldX += Speed * dt;


        public void TryJump(float duration)
        {
            if (IsJumping) return;
            IsJumping = true;
            JumpDuration = duration;
            JumpTimer = 0f;
        }


        public void TriggerHitFlash()
        {
            _hitFlashTimer = HitFlashDuration;
        }


        public const float JumpPeakPixels = 120f; // how high to rise in screen pixels

        public void UpdateAnimation(float dt)
        {
            if (IsJumping)
            {
                JumpTimer += dt;
                if (JumpTimer >= JumpDuration)
                {
                    IsJumping = false;
                    JumpTimer = 0f;
                    JumpYOffset = 0f;
                }
                else
                {
                    float t = JumpTimer / JumpDuration;
                    JumpYOffset = JumpPeakPixels * MathF.Sin(t * MathF.PI);
                }
            }
            else
            {
                JumpYOffset = 0f;
            }

            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= dt;

                float phase = _hitFlashTimer % (HitFlashInterval * 2f);
                bool visible = phase >= HitFlashInterval;

                if (Animator != null)
                {
                    Texture = visible ? Animator.GetStandSprite() : null!;
                }

                Color = visible ? Color.White : Color.FromArgb(0, 255, 255, 255);

                return;
            }

            Color = Color.White;
            Animator?.Update(dt, Speed, IsJumping);
            if (Animator != null) Texture = Animator.CurrentTexture;
        }
    }
}