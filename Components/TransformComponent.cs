using Microsoft.Xna.Framework;

namespace ZooTycoonManager.Components
{
    public class TransformComponent : Component
    {
        private Vector2 _position;

        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                Vector2 tilePos = GameWorld.PixelToTile(value);
                TileX = (int)tilePos.X;
                TileY = (int)tilePos.Y;
            }
        }

        public int TileX { get; set; }
        public int TileY { get; set; }
        public float Scale { get; set; } = 1f;
        public float Rotation { get; set; }

        public TransformComponent(Vector2 startPosition)
        {
            Position = startPosition;
        }
    }
}
