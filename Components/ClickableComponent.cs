using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ZooTycoonManager.Components
{
    public class ClickableComponent : Component
    {
        private TransformComponent _transform;
        private RenderComponent _render;
        private MouseState _previousMouseState;

        public event Action OnClick;

        public override void Initialize()
        {
            _transform = Owner.Transform;
            Owner.TryGetComponent(out _render);
        }

        public override void Update(GameTime gameTime)
        {
            MouseState currentMouseState = Mouse.GetState();
            Point mousePosition = new Point(currentMouseState.X, currentMouseState.Y);

            Rectangle bounds;
            if (_render != null)
            {
                var texture = _render.Texture;
                if (texture != null)
                {
                    float scaleX = _render.Width / texture.Width;
                    float scaleY = _render.Height / texture.Height;
                    int x = (int)(_transform.Position.X - _render.Origin.X * scaleX);
                    int y = (int)(_transform.Position.Y - _render.Origin.Y * scaleY);
                    bounds = new Rectangle(x, y, (int)_render.Width, (int)_render.Height);
                }
                else
                {
                    return;
                }
            }
            else
            {
                bounds = new Rectangle((int)_transform.Position.X, (int)_transform.Position.Y, 32, 32);
            }
            if (bounds.Contains(mousePosition) && currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                OnClick?.Invoke();
            }
            _previousMouseState = currentMouseState;
        }
    }
}
