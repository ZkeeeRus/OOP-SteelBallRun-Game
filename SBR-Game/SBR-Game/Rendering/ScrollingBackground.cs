using OpenTK.Mathematics;

namespace SBR_Game.Rendering
{
    public class ScrollingBackground : IDisposable
    {
        private readonly List<Texture2D> _textures = new();
        private readonly GameObject _slideA;
        private readonly GameObject _slideB;

        public ScrollingBackground(string backgroundsPath)
        {
            var files = Directory.GetFiles(backgroundsPath, "*.png")
                .Concat(Directory.GetFiles(backgroundsPath, "*.jpg"))
                .ToArray();

            if (files.Length == 0)
                throw new Exception($"No background images found in {backgroundsPath}");

            foreach (var file in files)
                _textures.Add(Texture2D.LoadFromFile(file));

            _slideA = new GameObject(_textures[0]) { ScaleMode = ScaleMode.Stretch };
            _slideB = new GameObject(_textures[0]) { ScaleMode = ScaleMode.Stretch };
        }

        public float GetSlideWidth(float screenWidth, float screenHeight)
        {
            ComputeSlideLayout(screenWidth, screenHeight, _textures[0], out float slideWidth, out _);
            return slideWidth;
        }

        public (GameObject current, GameObject next) GetSlides(
            float viewportX, float viewportY,
            float screenWidth, float screenHeight,
            float cameraWorldX,
            float texVMin = 0f, float texVMax = 1f)
        {
            ComputeSlideLayout(screenWidth, screenHeight, _textures[0], out float slideWidth, out float slideHeight);

            float normalized = cameraWorldX % slideWidth;
            if (normalized < 0) normalized += slideWidth;

            int tileIndex = (int)Math.Floor((double)cameraWorldX / slideWidth);
            float x1 = slideWidth / 2f - normalized;
            float x2 = x1 + slideWidth;
            float cy = viewportY + screenHeight / 2f;

            ConfigureSlide(_slideA, TextureAt(tileIndex), x1, cy, slideWidth, slideHeight, texVMin, texVMax);
            ConfigureSlide(_slideB, TextureAt(tileIndex + 1), x2, cy, slideWidth, slideHeight, texVMin, texVMax);

            return (_slideA, _slideB);
        }

        private static void ConfigureSlide(
            GameObject slide, Texture2D tex,
            float x, float y, float w, float h,
            float texVMin, float texVMax)
        {
            slide.Texture = tex;
            slide.Position = new Vector2(x, y);
            slide.Width = w;
            slide.Height = h;
            slide.TexCoordMin = new Vector2(0f, texVMin);
            slide.TexCoordMax = new Vector2(1f, texVMax);
        }

        private static void ComputeSlideLayout(
            float screenWidth, float screenHeight, Texture2D tex,
            out float slideWidth, out float slideHeight)
        {
            float texAspect = (float)tex.Width / tex.Height;
            slideHeight = screenHeight;
            slideWidth = slideHeight * texAspect;

            if (slideWidth < screenWidth)
            {
                slideWidth = screenWidth;
                slideHeight = screenWidth / texAspect;
            }
        }

        private Texture2D TextureAt(int tileIndex)
        {
            int n = _textures.Count;
            return _textures[((tileIndex % n) + n) % n];
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var t in _textures)
                t.Dispose();
        }
    }
}