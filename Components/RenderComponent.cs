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
        public float Width { get; set; }
        public float Height { get; set; }
        public Texture2D Texture { get => _texture; set => _texture = value; }
        public Vector2 Origin { get; set; } = Vector2.Zero;

        public RenderComponent(string texturePath)
        {
            TexturePath = texturePath;
        }

        public override void LoadContent(ContentManager contentManager)
        {
            _texture = contentManager.Load<Texture2D>(TexturePath);
            Width = _texture.Width;
            Height = _texture.Height;
            Origin = new Vector2(Width, Height) / 2;
        }

        public override void Initialize()
        {
            _transform = Owner.Transform;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 scale = new Vector2(Width / _texture.Width, Height / _texture.Height);
            spriteBatch.Draw(_texture, _transform.Position, SourceRectangle, Color.White, _transform.Rotation, Origin, scale, SpriteEffects.None, 0);
        }
    }
}
