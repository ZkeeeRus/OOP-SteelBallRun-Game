using SBR_Game.Rendering;

namespace SBR_Game.Gameplay.Bonuses
{
    public class BonusPickup : GameObject
    {
        public float WorldX { get; set; }
        public BonusDefinition Definition { get; }
        public bool IsCollected { get; private set; }

        public Texture2D? EffectSprite { get; set; }
        public Action? OnCollectSound { get; set; }

        private float _bobTimer;
        private const float BobAmplitude = 6f;
        private const float BobSpeed = 3f;

        public float BobOffset => BobAmplitude * MathF.Sin(_bobTimer * BobSpeed);

        public BonusPickup(Texture2D iconTexture, BonusDefinition definition) : base(iconTexture)
        {
            Definition = definition;
            ScaleMode = ScaleMode.KeepAspectRatio;
        }

        public void Update(float dt) => _bobTimer += dt;

        public void Collect()
        {
            IsCollected = true;
            OnCollectSound?.Invoke();
        }
    }
}