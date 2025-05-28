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

        public MoneyDisplay(SpriteFont font, Vector2 position, Color textColor, float scale = 1f)
        {
            _font = font;
            _position = position;
            _textColor = textColor;
            _scale = scale;
            MoneyText = string.Empty;
        }

        public void Update(decimal newMoneyAmount)
        {
            MoneyText = string.Format(CultureInfo.CurrentCulture, "Money: {0:N0}", newMoneyAmount);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_font != null && !string.IsNullOrEmpty(MoneyText))
            {
                spriteBatch.DrawString(_font, MoneyText, _position, _textColor, 0f, Vector2.Zero, _scale, SpriteEffects.None, 0);
            }
        }
    }
} 