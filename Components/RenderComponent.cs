using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ZooTycoonManager.Components
{
    public class RenderComponent : Component
    {
        private Texture2D _texture;
        private TransformComponent _transform;
        public string TexturePath { get; set; }

        public Rectangle? SourceRectangle { get; set; } = null;
        public float Width { get; private set; } = 0f;
        public float Height { get; private set; } = 0f;
        public Texture2D Texture { get => _texture; set => _texture = value; }
        public Vector2 Origin { get; set; } = Vector2.Zero;
        public bool CenterOrigin { get; set; }

        public RenderComponent(string texturePath)
        {
            TexturePath = texturePath;
            _texture = GameWorld.Instance.Content.Load<Texture2D>(TexturePath);
            Width = _texture.Width;
            Height = _texture.Height;
        }

        public void SetSize(Vector2 size)
        {
            Width = size.X;
            Height = size.Y;
            //Origin = new Vector2(Width, Height) / 2;
        }

        public override void Initialize()
        {
            _transform = Owner.Transform;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float sourceWidth = SourceRectangle?.Width ?? _texture.Width;
            float sourceHeight = SourceRectangle?.Height ?? _texture.Height;
            Vector2 scale = new Vector2(Width / sourceWidth, Height / sourceHeight);

            Vector2 originToUse = Origin;
            if (CenterOrigin)
            {
                if (SourceRectangle.HasValue)
                {
                    originToUse = new Vector2(SourceRectangle.Value.Width / 2f, SourceRectangle.Value.Height / 2f);
                }
                else
                {
                    originToUse = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
                }
            }

            spriteBatch.Draw(_texture, _transform.Position, SourceRectangle, Color.White, _transform.Rotation, originToUse, scale, SpriteEffects.None, 0);
        }
    }
}
