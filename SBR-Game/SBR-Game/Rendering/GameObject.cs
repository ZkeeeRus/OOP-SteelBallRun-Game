using OpenTK.Mathematics;
using System.Drawing;

namespace SBR_Game.Rendering
{
    public class GameObject
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public ScaleMode ScaleMode { get; set; } = ScaleMode.Stretch;
        public Vector2 TexCoordMin { get; set; } = Vector2.Zero;
        public Vector2 TexCoordMax { get; set; } = Vector2.One;
        public Color Color { get; set; } = Color.White;

        public GameObject(Texture2D texture) => Texture = texture;

        public void GetVertices(
            float viewportX, float viewportY,
            float viewportWidth, float viewportHeight,
            out float[] vertices, out uint[] indices)
        {
            float displayWidth = Width;
            float displayHeight = Height;

            if (ScaleMode == ScaleMode.KeepAspectRatio && Texture != null)
            {
                float texAspect = (float)Texture.Width / Texture.Height;
                float boxAspect = Width / Height;

                if (texAspect > boxAspect)
                {
                    displayWidth = Width;
                    displayHeight = Width / texAspect;
                }
                else
                {
                    displayHeight = Height;
                    displayWidth = Height * texAspect;
                }
            }

            float halfW = displayWidth / 2f;
            float halfH = displayHeight / 2f;

            float ndcLeft = ((Position.X - halfW - viewportX) / viewportWidth) * 2f - 1f;
            float ndcRight = ((Position.X + halfW - viewportX) / viewportWidth) * 2f - 1f;
            float ndcBottom = ((Position.Y - halfH - viewportY) / viewportHeight) * 2f - 1f;
            float ndcTop = ((Position.Y + halfH - viewportY) / viewportHeight) * 2f - 1f;

            vertices = new float[]
            {
                ndcRight, ndcTop,    0f,  TexCoordMax.X, TexCoordMax.Y,
                ndcRight, ndcBottom, 0f,  TexCoordMax.X, TexCoordMin.Y,
                ndcLeft,  ndcBottom, 0f,  TexCoordMin.X, TexCoordMin.Y,
                ndcLeft,  ndcTop,    0f,  TexCoordMin.X, TexCoordMax.Y
            };
            indices = new uint[] { 0, 1, 3, 1, 2, 3 };
        }
    }
}
