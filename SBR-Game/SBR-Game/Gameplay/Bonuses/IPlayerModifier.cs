using SBR_Game.Rendering;

namespace SBR_Game.Gameplay.Bonuses
{
    public interface IPlayerModifier
    {
        string Id { get; }
        float TimeLeft { get; }
        bool IsExpired { get; }

        float ModifyMaxSpeed(float baseMax);

        float ModifyAcceleration(float baseAccel);

        bool GrantsInvincibility { get; }

        Texture2D? EffectSprite { get; }

        void Update(float dt);
    }
}