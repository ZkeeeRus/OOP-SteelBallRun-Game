using SBR_Game.Rendering;
using System.Drawing;
using System.Windows.Forms;

namespace SBR_Game.Rendering.UI
{
    public class HudRenderer
    {
        // P1 = orange, P2 = blue
        private static readonly Color P1Color = Color.FromArgb(255, 255, 140, 40);
        private static readonly Color P2Color = Color.FromArgb(255, 40, 180, 255);
        private static readonly Color ShadowColor = Color.FromArgb(200, 0, 0, 0);

        private const int MarginX = 14;
        private const int MarginY = 12;
        private const int LineStep = 22;


        private readonly TextRenderer _textRenderer = new();

        public void Initialize() => _textRenderer.Initialize();


        public void Draw(float screenWidth, float screenHeight, float p1Distance, int p1Score, float p2Distance, int p2Score, float? goalDistance = null)
        {
            DrawPlayerBlock(screenWidth, screenHeight, MarginX, MarginY, "Джайро [W - прыжок]", p1Distance, p1Score, P1Color, goalDistance);

            int bottomY = (int)(screenHeight - MarginY - LineStep * 2 - 10);
            DrawPlayerBlock(screenWidth, screenHeight, MarginX, bottomY, "Джонни [↑ - прыжок]", p2Distance, p2Score, P2Color, goalDistance);
        }

        private void DrawPlayerBlock(float sw, float sh, int x, int y, string label, float distance, int score, Color color, float? goalDistance)
        {
            string distText = $"{label}  {distance / 1000f:F2} км.";
            if (goalDistance.HasValue && goalDistance.Value > 0)
            {
                float pct = Math.Clamp(distance / goalDistance.Value * 100f, 0f, 100f);
                distText += $"  ({pct:F0}%)";
            }

            string scoreText = $"Очки: {score}";

            _textRenderer.DrawText(distText, x + 1, y + 1, ShadowColor, sw, sh);
            _textRenderer.DrawText(scoreText, x + 1, y + LineStep + 1, ShadowColor, sw, sh);
            _textRenderer.DrawText(distText, x, y, color, sw, sh);
            _textRenderer.DrawText(scoreText, x, y + LineStep, color, sw, sh);
        }

        public void Dispose() => _textRenderer.Dispose();
    }
}