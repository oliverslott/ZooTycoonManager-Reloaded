using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ZooTycoonManager.Components
{
    public class TextRenderComponent : Component
    {
        private SpriteFont _font;
        private TransformComponent _transform;

        public string Text { get; set; }
        public Color Color { get; set; } = Color.Black;
        public string FontPath { get; set; }
        public Vector2 Offset { get; set; } = Vector2.Zero;

        public TextRenderComponent(string text, string fontPath)
        {
            Text = text;
            FontPath = fontPath;
        }

        public override void Initialize()
        {
            _transform = Owner.Transform;
        }

        public override void LoadContent(ContentManager contentManager)
        {
            _font = contentManager.Load<SpriteFont>(FontPath);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 textSize = _font.MeasureString(Text);
            Vector2 textPosition = _transform.Position - textSize / 2 + Offset;
            spriteBatch.DrawString(_font, Text, textPosition, Color);
        }
    }
}
