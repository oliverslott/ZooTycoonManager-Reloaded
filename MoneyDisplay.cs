using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZooTycoonManager
{
    public class MoneyDisplay : IObserver
    {
        public string MoneyText { get; private set; }
        private SpriteFont _font;
        private Vector2 _position;
        private Color _textColor;
        private float _scale;
        private Texture2D _background;
        private Vector2 _bgOffset;
        private Vector2 _bgScale;

        public MoneyDisplay(SpriteFont font, Vector2 position, Color textColor, float scale = 1f, Texture2D background = null, Vector2? bgOffset = null, Vector2? bgScale = null)
        {
            _font = font;
            _position = position;
            _textColor = textColor;
            _scale = scale;
            MoneyText = string.Empty;

            _background = background;
            _bgOffset = bgOffset ?? Vector2.Zero;
            _bgScale = bgScale ?? Vector2.One;
        }

        public void Update(decimal newMoneyAmount)
        {
            MoneyText = string.Format(CultureInfo.CurrentCulture, "{0:N0} $", newMoneyAmount);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_background != null)
            {
                spriteBatch.Draw(_background, _position + _bgOffset, null, Color.White, 0f, Vector2.Zero, _bgScale, SpriteEffects.None, 0);
            }

            if (_font != null && !string.IsNullOrEmpty(MoneyText))
            {
                Vector2 textSize = _font.MeasureString(MoneyText) * _scale;
                Vector2 bgSize = (_background != null)
                    ? new Vector2(_background.Width, _background.Height) * _bgScale
                    : textSize;

                Vector2 textOffset = (bgSize - textSize) / 2f - new Vector2(0, 4);

                spriteBatch.DrawString(_font, MoneyText, _position + _bgOffset + textOffset, _textColor, 0f, Vector2.Zero, _scale, SpriteEffects.None, 0);
            }
        }
    }
        
    
} 