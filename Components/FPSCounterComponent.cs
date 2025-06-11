using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager.Components
{
    public class FPSCounterComponent : Component
    {
        private float _fps;
        private float _frameTime;
        private int _frameCount;
        private float _elapsedTime;

        private TextRenderComponent _textRenderComponent;

        public override void Initialize()
        {
            _textRenderComponent = Owner.GetComponent<TextRenderComponent>();
        }

        public override void Update(GameTime gameTime)
        {
            _frameTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsedTime += _frameTime;
            _frameCount++;

            if (_elapsedTime >= 1.0f)
            {
                _fps = _frameCount / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0;

                _textRenderComponent.Text = $"FPS: {_fps:F1}";
            }
        }
    }
}
