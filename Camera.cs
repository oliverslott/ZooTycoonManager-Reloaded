using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ZooTycoonManager
{
    public class Camera
    {
        // Camera settings
        private Vector2 _cameraPosition;
        private Vector2 _lastMousePosition;
        private bool _isDragging;
        private const float CAMERA_MOVE_SPEED = 25f;
        private float _zoomScale;
        private const float MIN_ZOOM = 0.1f;
        private const float MAX_ZOOM = 2.0f;
        private const float ZOOM_SPEED = 0.1f;

        private GraphicsDeviceManager _graphics;

        public Camera(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
            // Initialize camera
            _cameraPosition = Vector2.Zero;
            _isDragging = false;
            _zoomScale = 1.0f;
        }

        // Convert screen coordinates to world coordinates
        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            Vector2 screenCenter = new Vector2(
                _graphics.PreferredBackBufferWidth / 2f,
                _graphics.PreferredBackBufferHeight / 2f
            );

            // Inverse of: P_s = (P_w - _cameraPosition) * _zoomScale + screenCenter
            // P_w = (P_s - screenCenter) / _zoomScale + _cameraPosition
            return (screenPos - screenCenter) / _zoomScale + _cameraPosition;
        }

        public void Update(GameTime gameTime, MouseState mouse, MouseState prevMouseState, KeyboardState keyboard, KeyboardState prevKeyboardState)
        {
            // Handle zoom with mouse wheel
            int scrollWheelDelta = mouse.ScrollWheelValue - prevMouseState.ScrollWheelValue;
            if (scrollWheelDelta != 0)
            {
                float zoomDelta = scrollWheelDelta > 0 ? ZOOM_SPEED : -ZOOM_SPEED;
                _zoomScale = MathHelper.Clamp(_zoomScale + zoomDelta, MIN_ZOOM, MAX_ZOOM);
            }

            // Handle camera movement with arrow keys
            if (keyboard.IsKeyDown(Keys.Left))
                _cameraPosition.X -= CAMERA_MOVE_SPEED;
            if (keyboard.IsKeyDown(Keys.Right))
                _cameraPosition.X += CAMERA_MOVE_SPEED;
            if (keyboard.IsKeyDown(Keys.Up))
                _cameraPosition.Y -= CAMERA_MOVE_SPEED;
            if (keyboard.IsKeyDown(Keys.Down))
                _cameraPosition.Y += CAMERA_MOVE_SPEED;

            // Handle camera movement with middle mouse button
            if (mouse.MiddleButton == ButtonState.Pressed)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                }
                else
                {
                    Vector2 currentMousePosition = new Vector2(mouse.X, mouse.Y);
                    Vector2 delta = currentMousePosition - _lastMousePosition;
                    _cameraPosition -= delta / _zoomScale;
                    _lastMousePosition = currentMousePosition;
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        public Matrix GetTransformMatrix()
        {
            Vector2 screenCenter = new Vector2(
                _graphics.PreferredBackBufferWidth / 2f, 
                _graphics.PreferredBackBufferHeight / 2f
            );

            // 1. Translate world so _cameraPosition is at origin
            // 2. Scale around the new origin
            // 3. Translate the new origin to the screenCenter
            return Matrix.CreateTranslation(-_cameraPosition.X, -_cameraPosition.Y, 0f) *
                   Matrix.CreateScale(_zoomScale, _zoomScale, 1f) *
                   Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0f);
        }

        public void UpdateViewport(Viewport viewport)
        {
            // Update the camera's view of the screen dimensions
            _graphics.PreferredBackBufferWidth = viewport.Width;
            _graphics.PreferredBackBufferHeight = viewport.Height;
            _graphics.ApplyChanges();
        }
    }
} 