using System.Text.Json;

namespace SBR_Game.Gameplay.Bonuses
{
    public static class BonusRegistry
    {
        private static List<BonusDefinition>? _all;

        public static IReadOnlyList<BonusDefinition> All => _all
            ?? throw new InvalidOperationException("BonusRegistry not loaded yet.");

        public static void Load(string contentRoot)
        {
            string path = Path.Combine(contentRoot, "Bonuses", "bonuses.json");
            if (!File.Exists(path))
            {
                _all = new List<BonusDefinition>();
                return;
            }

            string json = File.ReadAllText(path);
            _all = JsonSerializer.Deserialize<List<BonusDefinition>>(json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<BonusDefinition>();
        }

        public static BonusDefinition? Find(string id) => All.FirstOrDefault(b => b.Id == id);
    }
}