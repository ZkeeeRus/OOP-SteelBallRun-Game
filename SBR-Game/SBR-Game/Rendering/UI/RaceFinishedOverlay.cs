using SBR_Game.Rendering;
using System.Drawing;

namespace SBR_Game.Rendering.UI
{
    public class RaceFinishedOverlay
    {
        // P1 = orange, P2 = blue
        private static readonly Color WinColor1 = Color.FromArgb(255, 255, 140, 40);
        private static readonly Color WinColor2 = Color.FromArgb(255, 40, 180, 255);
        private static readonly Color SubColor = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color ShadowColor = Color.FromArgb(100, 0, 0, 0);

        private readonly TextRenderer _textRenderer = new();

        public void Initialize() => _textRenderer.Initialize();

        public void Draw(float screenWidth, float screenHeight, float p1Distance, int p1Score, float p2Distance, int p2Score)
        {
            bool p1Wins = p1Distance >= p2Distance;
            string winner = p1Wins ? "Джайро победил!" : "Джонни победил!";
            Color winColor = p1Wins ? WinColor1 : WinColor2;

            int cx = (int)(screenWidth / 2f) - 120;
            int cy = (int)(screenHeight * 0.38f);

            // Winner line
            _textRenderer.DrawText(winner, cx + 2, cy + 2, ShadowColor, screenWidth, screenHeight);
            _textRenderer.DrawText(winner, cx, cy, winColor, screenWidth, screenHeight);

            // Stats
            string stats1 = $"Джайро  {p1Distance / 1000f:F2} км.   Очки: {p1Score}";
            string stats2 = $"Джонни  {p2Distance / 1000f:F2} км.   Очки: {p2Score}";
            int sy = cy + 48;

            _textRenderer.DrawText(stats1, cx + 2, sy + 2, ShadowColor, screenWidth, screenHeight);
            _textRenderer.DrawText(stats1, cx, sy, SubColor, screenWidth, screenHeight);
            _textRenderer.DrawText(stats2, cx + 2, sy + 26 + 2, ShadowColor, screenWidth, screenHeight);
            _textRenderer.DrawText(stats2, cx, sy + 26, SubColor, screenWidth, screenHeight);

            // Prompt
            string prompt = "Нажмите Enter чтобы вернуться в главное меню";
            int px = (int)(screenWidth / 2f) - 270;
            int py = cy + 120;
            _textRenderer.DrawText(prompt, px + 2, py + 2, ShadowColor, screenWidth, screenHeight);
            _textRenderer.DrawText(prompt, px, py, SubColor, screenWidth, screenHeight);
        }

        public void Dispose() => _textRenderer.Dispose();
    }
}