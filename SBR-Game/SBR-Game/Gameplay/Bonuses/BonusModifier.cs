using SBR_Game.Rendering;

namespace SBR_Game.Gameplay.Bonuses
{
    public class BonusModifier : IPlayerModifier
    {
        private readonly BonusDefinition _def;
        private float _timeLeft;

        public string Id => _def.Id;
        public float TimeLeft => _timeLeft;
        public bool IsExpired => _timeLeft <= 0f;

        public bool GrantsInvincibility => _def.GrantsInvincibility;
        public Texture2D? EffectSprite { get; }

        public BonusModifier(BonusDefinition def, Texture2D? effectSprite = null)
        {
            _def = def;
            _timeLeft = def.Duration;
            EffectSprite = effectSprite;
        }

        public float ModifyMaxSpeed(float baseMax) => baseMax * _def.MaxSpeedMultiplier;

        public float ModifyAcceleration(float baseAccel) => baseAccel * _def.AccelerationMultiplier;

        public void Update(float dt)
        {
            if (_timeLeft > 0f) _timeLeft -= dt;
        }
    }
}