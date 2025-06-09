using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ZooTycoonManager.UI
{
    public class FPSCounter
    {
        private float _fps;
        private float _frameTime;
        private int _frameCount;
        private float _elapsedTime;
        private SpriteFont _font;
        private Vector2 _position;
        private Color _color;
        private GraphicsDeviceManager _graphics;

        public FPSCounter(SpriteFont font, GraphicsDeviceManager graphics)
        {
            _font = font;
            _graphics = graphics;
            _color = Color.White;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            _position = new Vector2(_graphics.PreferredBackBufferWidth - 100, 10);
        }

        public void Update(GameTime gameTime)
        {
            _frameTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsedTime += _frameTime;
            _frameCount++;

            if (_elapsedTime >= 1.0f)
            {
                _fps = _frameCount / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0;
            }

            UpdatePosition();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            string fpsText = $"FPS: {_fps:F1}";
            spriteBatch.DrawString(_font, fpsText, _position, _color);
        }
    }
} 