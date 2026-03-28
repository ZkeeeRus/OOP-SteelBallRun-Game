using OpenTK.Mathematics;
using SBR_Game.Rendering;

public class ScrollingBackground : IDisposable
{
    private readonly List<Texture2D> _textures = new();
    private Texture2D _currentTexture;
    private Texture2D _nextTexture;
    private float _offsetX = 0f;
    private readonly Random _random = new();

    // ✅ Переиспользуемые объекты — создаются один раз
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

        _currentTexture = GetRandom(null);
        _nextTexture = GetRandom(_currentTexture);

        // ✅ Создаём объекты один раз — просто меняем их свойства каждый кадр
        _slideA = new GameObject(_currentTexture) { ScaleMode = ScaleMode.Stretch };
        _slideB = new GameObject(_nextTexture) { ScaleMode = ScaleMode.Stretch };
    }

    private Texture2D GetRandom(Texture2D? exclude)
    {
        if (_textures.Count == 1) return _textures[0];
        Texture2D result;
        do { result = _textures[_random.Next(_textures.Count)]; }
        while (result == exclude);
        return result;
    }

    public void Scroll(float deltaPixels)
    {
        _offsetX += deltaPixels;
    }

    public (GameObject current, GameObject next) GetSlides(float screenWidth, float screenHeight)
    {
        float texAspect = (float)_currentTexture.Width / _currentTexture.Height;
        float slideHeight = screenHeight;
        float slideWidth = slideHeight * texAspect;

        if (slideWidth < screenWidth)
        {
            slideWidth = screenWidth;
            slideHeight = screenWidth / texAspect;
        }

        float normalized = _offsetX % slideWidth;
        if (normalized < 0) normalized += slideWidth;

        // Смена фонов когда слайд полностью ушёл
        if (_offsetX != 0 && normalized < 1f)
        {
            _currentTexture = _nextTexture;
            _nextTexture = GetRandom(_currentTexture);
        }

        float x1 = slideWidth / 2f - normalized;
        float x2 = x1 + slideWidth;

        // ✅ Выравниваем по низу
        float cy = screenHeight - slideHeight / 2f;

        // ✅ Обновляем свойства существующих объектов
        _slideA.Texture = _currentTexture;
        _slideA.Position = new Vector2(x1, cy);
        _slideA.Width = slideWidth;
        _slideA.Height = slideHeight;

        _slideB.Texture = _nextTexture;
        _slideB.Position = new Vector2(x2, cy);
        _slideB.Width = slideWidth;
        _slideB.Height = slideHeight;

        return (_slideA, _slideB);
    }

    public void DebugBindTexture()
    {
        _currentTexture.Use();
    }

    public void Dispose()
    {
        foreach (var t in _textures)
            t.Dispose();
    }
}