using OpenTK.Mathematics;

namespace SBR_Game.Rendering
{
    public class GameObject
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public ScaleMode ScaleMode { get; set; } = ScaleMode.Stretch;
        public bool IsBackground { get; set; } = false;
        public Vector2 TexCoordOffset { get; set; } = Vector2.Zero;

        public GameObject(Texture2D texture)
        {
            Texture = texture;
        }

        public void SetPosition(float x, float y)
        {
            Position = new Vector2(x, y);
        }

        public void Move(float dx, float dy)
        {
            Position = new Vector2(Position.X + dx, Position.Y + dy);
        }

        public void GetVertices(float screenWidth, float screenHeight, out float[] vertices, out uint[] indices)
        {
            float displayWidth = Width;
            float displayHeight = Height;

            // KeepAspectRatio только если явно задан
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
            // Stretch — используем Width/Height как есть, без изменений

            float halfW = displayWidth / 2f;
            float halfH = displayHeight / 2f;

            float left = Position.X - halfW;
            float right = Position.X + halfW;
            float bottom = Position.Y - halfH;
            float top = Position.Y + halfH;

            float ndcLeft = (left / screenWidth) * 2f - 1f;
            float ndcRight = (right / screenWidth) * 2f - 1f;
            float ndcBottom = (bottom / screenHeight) * 2f - 1f;
            float ndcTop = (top / screenHeight) * 2f - 1f;

            vertices = new float[]
            {
        ndcRight, ndcTop,    0f,  1f, 0f,
        ndcRight, ndcBottom, 0f,  1f, 1f,
        ndcLeft,  ndcBottom, 0f,  0f, 1f,
        ndcLeft,  ndcTop,    0f,  0f, 0f
            };
            indices = new uint[] { 0, 1, 3, 1, 2, 3 };
        }
    }
}