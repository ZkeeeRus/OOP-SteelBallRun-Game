using SBR_Game.Gameplay;
using SBR_Game.Gameplay.Bonuses;

namespace SBR_Game.Core
{
    public class PlayerState
    {
        public Player Player { get; }
        public WarningEffect Warning { get; }
        public List<Obstacle> Obstacles { get; } = new();
        public float NextObstacleWorldX { get; set; }
        public Obstacle? LastWarnedObstacle { get; set; }

        public int Score { get; set; }
        public float FinalDistance { get; set; } = -1f;
        public float Distance => FinalDistance >= 0 ? FinalDistance : Player.WorldX;
        public bool HasFinished => FinalDistance >= 0;

        public List<IPlayerModifier> ActiveBonuses { get; } = new();
        public List<BonusPickup> BonusPickups { get; } = new();
        public Action? OnObstacleHit { get; set; }

        public float EffectiveMaxSpeed(float baseMax)
        {
            float v = baseMax;
            foreach (IPlayerModifier b in ActiveBonuses)
                v = b.ModifyMaxSpeed(v);
            return v;
        }

        public float EffectiveAcceleration(float baseAccel)
        {
            float v = baseAccel;
            foreach (IPlayerModifier b in ActiveBonuses)
                v = b.ModifyAcceleration(v);
            return v;
        }

        public bool HasBonusInvincibility => ActiveBonuses.Any(b => b.GrantsInvincibility);

        public PlayerState(Player player, WarningEffect warning, float initialObstacleDistance)
        {
            Player = player;
            Warning = warning;
            NextObstacleWorldX = initialObstacleDistance;
        }
    }
}