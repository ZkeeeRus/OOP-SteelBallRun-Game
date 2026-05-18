using SBR_Game.Rendering;
using System.Drawing;

namespace SBR_Game.Rendering.UI
{
    public class RaceFinishedOverlay
    {

        private static readonly Color WinColor1 = Color.FromArgb(255, 255, 140, 40);
        private static readonly Color WinColor2 = Color.FromArgb(255, 40, 180, 255);
        private static readonly Color SubColor = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color ShadowColor = Color.FromArgb(100, 0, 0, 0);
        private static readonly Color GoldColor = Color.FromArgb(255, 255, 215, 0);
        private static readonly Color SilverColor = Color.FromArgb(255, 192, 192, 192);
        private static readonly Color BronzeColor = Color.FromArgb(255, 205, 127, 50);
        private static readonly Color NpcColor = Color.FromArgb(255, 200, 200, 200);

        private readonly TextRenderer _textRenderer = new();


        private static readonly string[] NpcNames = new[]
        {
            "Диего", "Хот Пантс", "Поколоко", "Ринго", "Сандман"
        };


        private record LeaderEntry(string Name, bool IsPlayer, int PlayerSlot, int Score);

        private readonly List<LeaderEntry> _entries = new();
        private readonly List<LeaderEntry> _pendingEntries = new();

        private float _timer = 0f;
        private bool _initialized = false;
        private bool _winnerShown = false;


        private const float EntryDelay = 0.9f;
        private float _nextEntryTimer = 0f;


        private const float NpcBeforeLoserChance = 0.55f;


        private int _npcBeforeLoserCount = 0;
        private bool _loserAdded = false;


        private int _npcAfterCount = 0;


        public int LoserFinalPlace { get; private set; } = 2;


        private int _winnerSlot;
        private float _p1Distance, _p2Distance;
        private int _p1Score, _p2Score;


        public bool IsFullyShown => _pendingEntries.Count == 0 && _initialized;

        public Action<int>? OnLoserPlaceDecided { get; set; }

        public void Initialize() => _textRenderer.Initialize();

        public void Draw(float screenWidth, float screenHeight, float p1Distance, int p1Score, float p2Distance, int p2Score, int winnerSlot, float dt)
        {
            if (!_initialized)
            {
                _winnerSlot = winnerSlot;
                _p1Distance = p1Distance;
                _p2Distance = p2Distance;
                _p1Score = p1Score;
                _p2Score = p2Score;
                BuildEntryQueue();
                _initialized = true;
            }

            _timer += dt;


            if (_pendingEntries.Count > 0)
            {
                _nextEntryTimer -= dt;
                if (_nextEntryTimer <= 0f)
                {
                    LeaderEntry entry = _pendingEntries[0];
                    _pendingEntries.RemoveAt(0);
                    _entries.Add(entry);
                    _nextEntryTimer = EntryDelay;
                }
            }

            RenderTable((int)screenWidth, (int)screenHeight);
        }

        private void BuildEntryQueue()
        {
            _entries.Clear();
            _pendingEntries.Clear();

            string winnerName = _winnerSlot == 1 ? "Джайро" : "Джонни";
            string loserName = _winnerSlot == 1 ? "Джонни" : "Джайро";
            float winnerDist = _winnerSlot == 1 ? _p1Distance : _p2Distance;
            float loserDist = _winnerSlot == 1 ? _p2Distance : _p1Distance;
            int winnerScore = _winnerSlot == 1 ? _p1Score : _p2Score;
            int loserScore = _winnerSlot == 1 ? _p2Score : _p1Score;


            List<string> pool = NpcNames.OrderBy(_ => Random.Shared.Next()).ToList();
            int npcIdx = 0;


            _pendingEntries.Add(new LeaderEntry(winnerName, true, _winnerSlot, winnerScore));


            _npcBeforeLoserCount = 0;
            if (Random.Shared.NextSingle() < NpcBeforeLoserChance)
                _npcBeforeLoserCount = Random.Shared.Next(0, 3);

            for (int i = 0; i < _npcBeforeLoserCount; i++)
            {
                string npcName = pool[npcIdx++ % pool.Count];
                int npcScore = Random.Shared.Next(100, 5000);
                _pendingEntries.Add(new LeaderEntry(npcName, false, 0, npcScore));
            }

            LoserFinalPlace = 2 + _npcBeforeLoserCount;
            _pendingEntries.Add(new LeaderEntry(loserName, true,
                _winnerSlot == 1 ? 2 : 1, loserScore));


            _npcAfterCount = Random.Shared.Next(2, 4);
            for (int i = 0; i < _npcAfterCount; i++)
            {
                string npcName = pool[npcIdx++ % pool.Count];
                int npcScore = Random.Shared.Next(100, 5000);
                _pendingEntries.Add(new LeaderEntry(npcName, false, 0, npcScore));
            }

            _nextEntryTimer = 0.2f;
            OnLoserPlaceDecided?.Invoke(LoserFinalPlace);
        }

        private void RenderTable(int W, int H)
        {
            int cx = W / 2 - 240;
            int cy = (int)(H * 0.18f);

            string title = "РЕЗУЛЬТАТЫ ГОНКИ";
            DrawShadow(title, cx + 90, cy, GoldColor, W, H);

            int rowY = cy + 52;
            int rowH = 34;

            for (int i = 0; i < _entries.Count; i++)
            {
                LeaderEntry e = _entries[i];
                int pos = i + 1;

                Color placeColor = pos switch
                {
                    1 => GoldColor,
                    2 => SilverColor,
                    3 => BronzeColor,
                    _ => NpcColor
                };

                string placeStr = $"{pos}.";
                string nameStr = e.Name;
                string scoreStr = $"Очки: {e.Score}";

                Color nameColor = e.IsPlayer
                    ? (e.PlayerSlot == 1 ? WinColor1 : WinColor2)
                    : NpcColor;

                int ry = rowY + i * rowH;

                DrawShadow(placeStr, cx, ry, placeColor, W, H);
                DrawShadow(nameStr, cx + 55, ry, nameColor, W, H);
                DrawShadow(scoreStr, cx + 230, ry, SubColor, W, H);
            }

            if (IsFullyShown)
            {
                string prompt = "Нажмите Enter чтобы вернуться в главное меню";
                int px = W / 2 - 270;
                int py = rowY + _entries.Count * rowH + 24;
                DrawShadow(prompt, px, py, SubColor, W, H);
            }
        }

        private void DrawShadow(string text, int x, int y, Color color, int W, int H)
        {
            _textRenderer.DrawText(text, x + 2, y + 2, ShadowColor, W, H);
            _textRenderer.DrawText(text, x, y, color, W, H);
        }

        public void Reset()
        {
            _initialized = false;
            _entries.Clear();
            _pendingEntries.Clear();
            _timer = 0f;
            _nextEntryTimer = 0f;
            LoserFinalPlace = 2;
        }

        public void Dispose() => _textRenderer.Dispose();
    }
}
