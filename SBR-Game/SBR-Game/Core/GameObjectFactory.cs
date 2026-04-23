using OpenTK.Mathematics;
using SBR_Game.Gameplay;
using SBR_Game.Gameplay.Bonuses;
using SBR_Game.Rendering;

namespace SBR_Game.Core
{
    public class GameObjectFactory : IDisposable
    {
        private readonly string _contentPath;
        private readonly Texture2D[] _bushTextures = new Texture2D[3];

        private readonly Dictionary<string, Texture2D> _textureCache = new();

        public GameObjectFactory(string contentPath)
        {
            _contentPath = contentPath;

            for (int i = 0; i < 3; i++)
            {
                string path = Path.Combine(contentPath, "Images", "Bushes", $"Bush_{i + 1}.png");
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Bush texture not found: {path}");
                _bushTextures[i] = Texture2D.LoadFromFile(path);
            }
        }


        public Player CreatePlayer(string textureName, float x, float y, float width, float height)
            => new Player(LoadTexture(textureName))
            {
                Position = new Vector2(x, y),
                Width = width,
                Height = height
            };

        public GameObject CreateSprite(string textureName, float x, float y, float width, float height, ScaleMode scaleMode = ScaleMode.Stretch)
            => new GameObject(LoadTexture(Path.Combine("Images", textureName)))
            {
                Position = new Vector2(x, y),
                Width = width,
                Height = height,
                ScaleMode = scaleMode
            };

        public Bush CreateBush(int level, float worldX, float y, float width, float height)
        {
            int index = Math.Clamp(level - 1, 0, 2);
            return new Bush(_bushTextures[index], level)
            {
                WorldX = worldX,
                Position = new Vector2(worldX, y),
                Width = width,
                Height = height
            };
        }

        public WarningEffect CreateWarning(float x, float y, float size)
            => new WarningEffect(LoadTexture(Path.Combine("Images", "Effects", "Warning.png")))
            {
                Position = new Vector2(x, y),
                Width = size,
                Height = size
            };

        public BonusPickup CreateBonusPickup(BonusDefinition def, float worldX, float size)
        {
            var iconTex = TryLoadCached(def.IconPath);
            if (iconTex == null)
                throw new FileNotFoundException($"Bonus icon not found: {def.IconPath}");

            Texture2D? effectTex = string.IsNullOrEmpty(def.EffectSpritePath) ? null : TryLoadCached(def.EffectSpritePath);

            return new BonusPickup(iconTex, def)
            {
                WorldX = worldX,
                Position = new Vector2(worldX, 0),
                Width = size,
                Height = size,
                EffectSprite = effectTex
            };
        }


        private Texture2D LoadTexture(string relativePath)
        {
            string full = Path.Combine(_contentPath, relativePath);
            if (!File.Exists(full))
                throw new FileNotFoundException($"Texture not found: {full}");
            return Texture2D.LoadFromFile(full);
        }

        private Texture2D? TryLoadCached(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (_textureCache.TryGetValue(relativePath, out var cached)) return cached;

            string full = Path.Combine(_contentPath, relativePath);
            if (!File.Exists(full)) return null;

            var tex = Texture2D.LoadFromFile(full);
            _textureCache[relativePath] = tex;
            return tex;
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var t in _bushTextures) t?.Dispose();
            foreach (var t in _textureCache.Values) t.Dispose();
        }
    }
}