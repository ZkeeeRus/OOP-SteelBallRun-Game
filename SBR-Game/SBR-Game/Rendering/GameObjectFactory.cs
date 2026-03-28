using OpenTK.Mathematics;

namespace SBR_Game.Rendering
{
    public class GameObjectFactory
    {
        private readonly string _contentPath;

        public GameObjectFactory(string contentPath)
        {
            _contentPath = contentPath;
        }

        public GameObject CreateSprite(string textureName, float x, float y, float width, float height, ScaleMode scaleMode = ScaleMode.Stretch)
        {
            string texturePath = System.IO.Path.Combine(_contentPath, "Images", textureName);
            var texture = Texture2D.LoadFromFile(texturePath);

            var gameObject = new GameObject(texture)
            {
                Position = new Vector2(x, y),
                Width = width,
                Height = height,
                ScaleMode = scaleMode
            };

            return gameObject;
        }
    }
}