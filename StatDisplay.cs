using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace ZooTycoonManager
{
    public class StatDisplay
    {
        private SpriteFont _font;
        private Vector2 _position;
        private Color _textColor;
        private float _scale;
        private Texture2D _background;
        private Vector2 _bgOffset;
        private Vector2 _bgScale;
        private string _text;

        public StatDisplay(SpriteFont font, Vector2 position, Color textColor, float scale, Texture2D background, Vector2 bgOffset, Vector2 bgScale)
        {
            _font = font;
            _position = position;
            _textColor = textColor;
            _scale = scale;
            _background = background;
            _bgOffset = bgOffset;
            _bgScale = bgScale;
            _text = "";
        }

        public void SetText(string text)
        {
            _text = text;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_background != null)
            {
                spriteBatch.Draw(_background, _position + _bgOffset, null, Color.White, 0f, Vector2.Zero, _bgScale, SpriteEffects.None, 0);
            }

            if (_font != null && !string.IsNullOrEmpty(_text))
            {
                Vector2 textSize = _font.MeasureString(_text) * _scale;
                Vector2 bgSize = new Vector2(_background.Width, _background.Height) * _bgScale;
                Vector2 textOffset = (bgSize - textSize) / 2;
                textOffset.Y -= 4;

                spriteBatch.DrawString(_font, _text, _position + _bgOffset + textOffset, _textColor, 0f, Vector2.Zero, _scale, SpriteEffects.None, 0);
            }
        }
    }

}
