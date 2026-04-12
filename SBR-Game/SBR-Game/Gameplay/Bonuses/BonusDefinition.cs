namespace SBR_Game.Gameplay.Bonuses
{
    public class BonusDefinition
    {
        public string Id { get; set; } = "";   // e.g. "speed_boost"
        public string DisplayName { get; set; } = "";
        public string IconPath { get; set; } = "";   // relative to Content/

        public string EffectSpritePath { get; set; } = "";

        public float Duration { get; set; } = 5f;

        public float MaxSpeedMultiplier { get; set; } = 1f;
        public float AccelerationMultiplier { get; set; } = 1f;
        public bool GrantsInvincibility { get; set; } = false;
    }
}