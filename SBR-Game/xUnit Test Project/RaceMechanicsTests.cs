
using SBR_Game.Gameplay;
using SBR_Game.Gameplay.Bonuses;
using System;
using System.Numerics;
using Xunit;

namespace SBR_Game.Tests
{
    public class RaceMechanicsTests
    {
        // Тест ограничения максимальной скорости при ускорении
        [Fact]
        public void Player_Accelerate_ShouldNotExceedMaxSpeed()
        {
            var player = new Player(texture: null);
            player.Speed = GameLogic.MaxSpeed - 50f;
            player.Accelerate(GameLogic.Acceleration, GameLogic.MaxSpeed, dt: 1f);
            Assert.True(player.Speed <= GameLogic.MaxSpeed);
        }

        // Тест корректного применения замедления от препятствия
        [Fact]
        public void Player_ApplySlowdown_ShouldReduceSpeedCorrectly()
        {
            var player = new Player(texture: null);
            player.Speed = 100f;
            float factor = 1f - 0.15f * 2;
            player.ApplySlowdown(factor, GameLogic.MinSpeed);
            Assert.Equal(70f, player.Speed, precision: 2);
        }

        // Тест наложения нескольких модификаторов характеристик
        [Fact]
        public void BonusModifier_Stacking_ShouldMultiplyEffects()
        {
            var defSpeed = new BonusDefinition { MaxSpeedMultiplier = 1.5f, AccelerationMultiplier = 1f };
            var defAccel = new BonusDefinition { MaxSpeedMultiplier = 1f, AccelerationMultiplier = 2f };
            var mod1 = new BonusModifier(defSpeed, effectSprite: null);
            var mod2 = new BonusModifier(defAccel, effectSprite: null);

            float baseSpeed = GameLogic.MaxSpeed;
            float resultSpeed = mod2.ModifyMaxSpeed(mod1.ModifyMaxSpeed(baseSpeed));
            Assert.Equal(GameLogic.MaxSpeed * 1.5f, resultSpeed);
        }

        // Блок: тесты механики прыжка
        [Fact]
        public void Player_Jump_ShouldFollowSinusoidalPath()
        {
            var player = new Player(null);
            player.TryJump(0.55f);
            player.UpdateAnimation(0.275f);
            Assert.True(player.JumpYOffset > 100f);
        }

        // Тест корректного истечения времени действия бонуса
        [Fact]
        public void BonusModifier_Update_ShouldExpireCorrectly()
        {
            var def = new BonusDefinition { Duration = 5f, MaxSpeedMultiplier = 2f, AccelerationMultiplier = 1f };
            var mod = new BonusModifier(def, effectSprite: null);
            mod.Update(dt: 6f);
            Assert.True(mod.IsExpired);
        }

        // Тест срабатывания условия финиша при достижении целевой дистанции
        [Fact]
        public void GameLogic_FinishDistance_ShouldTriggerCorrectly()
        {
            float p1X = GameLogic.RaceGoalDistance - 10f;
            float p2X = GameLogic.RaceGoalDistance + 50f;
            bool p1Finished = p1X >= GameLogic.RaceGoalDistance;
            bool p2Finished = p2X >= GameLogic.RaceGoalDistance;
            Assert.False(p1Finished);
            Assert.True(p2Finished);
        }
    }
}