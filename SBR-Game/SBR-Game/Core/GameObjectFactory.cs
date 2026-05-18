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
        private readonly Texture2D[] _barrierTextures = new Texture2D[3];
        private readonly Texture2D[] _lakeTextures = new Texture2D[3];
        private Texture2D? _finishTexture;

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

            for (int i = 0; i < 3; i++)
            {
                string path = Path.Combine(contentPath, "Images", "Barriers", $"Barrier_{i + 1}.png");
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Barrier texture not found: {path}");
                _barrierTextures[i] = Texture2D.LoadFromFile(path);
            }

            for (int i = 0; i < 3; i++)
            {
                string path = Path.Combine(contentPath, "Images", "Lakes", $"Lake_{i + 1}.png");
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Lake texture not found: {path}");
                _lakeTextures[i] = Texture2D.LoadFromFile(path);
            }

            string finishPath = Path.Combine(contentPath, "Images", "Finish.png");
            if (File.Exists(finishPath))
                _finishTexture = Texture2D.LoadFromFile(finishPath);
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

        public Bush CreateBush(int level, float worldX, float y, float width, float height, float yOffset = 0)
        {
            int index = Math.Clamp(level - 1, 0, 2);
            return new Bush(_bushTextures[index], level)
            {
                WorldX = worldX,
                WorldYOffset = yOffset,
                Position = new Vector2(worldX, y),
                Width = width,
                Height = height
            };
        }

        public Gameplay.Barrier CreateBarrier(int level, float worldX, float y, float width, float height, float yOffset = 0)
        {
            int index = Math.Clamp(level - 1, 0, 2);
            return new Gameplay.Barrier(_barrierTextures[index], level)
            {
                WorldX = worldX,
                WorldYOffset = yOffset,
                Position = new Vector2(worldX, y),
                Width = width,
                Height = height
            };
        }

        public Lake CreateLake(int level, float worldX, float y, float width, float height, float yOffset = 0)
        {
            int index = Math.Clamp(level - 1, 0, 2);
            return new Lake(_lakeTextures[index], level)
            {
                WorldX = worldX,
                WorldYOffset = yOffset,
                Position = new Vector2(worldX, y),
                Width = width,
                Height = height
            };
        }

        public GameObject? CreateFinishSprite(float worldX, float screenY, float width, float height)
        {
            if (_finishTexture == null) return null;
            return new GameObject(_finishTexture)
            {
                Position = new Vector2(worldX, screenY),
                Width = width,
                Height = height,
                ScaleMode = ScaleMode.KeepAspectRatio
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
            _finishTexture?.Dispose();
        }
    }
}