using OpenTK.Mathematics;
using SBR_Game.Rendering;

namespace SBR_Game.Gameplay
{
    public class Obstacle : GameObject
    {
        public int Level { get; }
        public float SlowdownFactor { get; }
        public float WorldX { get; set; }
        public float WorldYOffset { get; set; }

        public Obstacle(Texture2D texture, int level) : base(texture)
        {
            Level = level;
            SlowdownFactor = 0.3f + (level - 1) * 0.2f;
            ScaleMode = ScaleMode.KeepAspectRatio;
            TexCoordMin = new Vector2(0.02f, 0.02f);
            TexCoordMax = new Vector2(0.98f, 0.98f);
        }
    }

    public class Bush : Obstacle
    {
        public Bush(Texture2D texture, int level) : base(texture, level)
        {
            ScaleMode = ScaleMode.KeepAspectRatio;
        }
    }

    public class Barrier : Obstacle
    {
        public Barrier(Texture2D texture, int level) : base(texture, level)
        {
            ScaleMode = ScaleMode.KeepAspectRatio;
        }
    }

    public class Lake : Obstacle
    {
        public Lake(Texture2D texture, int level) : base(texture, level)
        {
            ScaleMode = ScaleMode.Stretch;
        }
    }
}
