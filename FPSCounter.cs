using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ZooTycoonManager
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

        public FPSCounter(SpriteFont font)
        {
            _font = font;
            _position = new Vector2(GameWorld.GRID_WIDTH * GameWorld.TILE_SIZE - 100, 10);
            _color = Color.White;
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
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            string fpsText = $"FPS: {_fps:F1}";
            spriteBatch.DrawString(_font, fpsText, _position, _color);
        }
    }
} 