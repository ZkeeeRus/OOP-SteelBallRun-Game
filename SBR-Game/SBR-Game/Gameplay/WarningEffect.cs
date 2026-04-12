using System.Drawing;
using SBR_Game.Rendering;

namespace SBR_Game.Gameplay
{
    public class WarningEffect : GameObject
    {
        public bool  IsActive { get; private set; }
        public float Timer    { get; private set; }
        public float Duration { get; private set; }

        public WarningEffect(Texture2D texture) : base(texture)
        {
            ScaleMode = ScaleMode.Stretch;
        }

        public void Activate(float duration = 1f)
        {
            Duration  = duration;
            Timer     = 0f;
            IsActive  = true;
        }

        public void Deactivate()
        {
            IsActive = false;
            Color    = Color.White;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            Timer += deltaTime;
            if (Timer >= Duration) { Deactivate(); return; }

            float alpha = Math.Clamp(0.4f + 0.6f * MathF.Sin(Timer * 8f), 0f, 1f);
            Color = Color.FromArgb((int)(alpha * 255), 255, 255, 0);
        }
    }
}
