using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ZooTycoonManager
{
    public class Camera
    {
        private Vector2 _cameraPosition;
        private Vector2 _lastMousePosition;
        private bool _isDragging;
        private const float CAMERA_MOVE_SPEED = 5f;
        private float _zoomScale;
        private const float MIN_ZOOM = 0.1f;
        private const float MAX_ZOOM = 2.0f;
        private const float ZOOM_SPEED = 0.1f;
        public const float CAMERA_BOUNDS_BUFFER = 10 * GameWorld.TILE_SIZE;

        private GraphicsDeviceManager _graphics;

        private float _mapWidthInPixels;
        private float _mapHeightInPixels;

        private bool _clampToMapBoundaries = true;

        public Camera(GraphicsDeviceManager graphics)
        {
            _graphics = graphics;
            _cameraPosition = Vector2.Zero;
            _isDragging = false;
            _zoomScale = 1.0f;
        }
        public Vector2 ScreenToWorld(Vector2 screenPos)
        {
            Vector2 screenCenter = new Vector2(
                _graphics.PreferredBackBufferWidth / 2f,
                _graphics.PreferredBackBufferHeight / 2f
            );

            return (screenPos - screenCenter) / _zoomScale + _cameraPosition;
        }

        public void SetMapDimensions(float mapWidth, float mapHeight)
        {
            _mapWidthInPixels = mapWidth;
            _mapHeightInPixels = mapHeight;
        }

        public void ToggleClamping()
        {
            _clampToMapBoundaries = !_clampToMapBoundaries;
        }

        public void Update(GameTime gameTime, MouseState mouse, MouseState prevMouseState, KeyboardState keyboard, KeyboardState prevKeyboardState)
        {
            // Handle zoom with mouse wheel
            int scrollWheelDelta = mouse.ScrollWheelValue - prevMouseState.ScrollWheelValue;
            if (scrollWheelDelta != 0)
            {
                float zoomDelta = scrollWheelDelta > 0 ? ZOOM_SPEED : -ZOOM_SPEED;
                float targetZoom = _zoomScale + zoomDelta;

                if (_clampToMapBoundaries)
                {
                    float minRequiredZoomBasedOnMap = MIN_ZOOM;
                    if (_mapWidthInPixels > 0 && _graphics.PreferredBackBufferWidth > 0)
                    {
                        minRequiredZoomBasedOnMap = Math.Max(minRequiredZoomBasedOnMap, (float)_graphics.PreferredBackBufferWidth / _mapWidthInPixels);
                    }
                    if (_mapHeightInPixels > 0 && _graphics.PreferredBackBufferHeight > 0)
                    {
                        minRequiredZoomBasedOnMap = Math.Max(minRequiredZoomBasedOnMap, (float)_graphics.PreferredBackBufferHeight / _mapHeightInPixels);
                    }


                    _zoomScale = MathHelper.Clamp(targetZoom, minRequiredZoomBasedOnMap, MAX_ZOOM);
                }
                else
                {
                    _zoomScale = MathHelper.Clamp(targetZoom, MIN_ZOOM, MAX_ZOOM);
                }
            }

            if (keyboard.IsKeyDown(Keys.Left))
                _cameraPosition.X -= CAMERA_MOVE_SPEED;
            if (keyboard.IsKeyDown(Keys.Right))
                _cameraPosition.X += CAMERA_MOVE_SPEED;
            if (keyboard.IsKeyDown(Keys.Up))
                _cameraPosition.Y -= CAMERA_MOVE_SPEED;
            if (keyboard.IsKeyDown(Keys.Down))
                _cameraPosition.Y += CAMERA_MOVE_SPEED;


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


            if (_clampToMapBoundaries && _mapWidthInPixels > 0 && _mapHeightInPixels > 0)
            {
                float currentZoom = Math.Max(_zoomScale, 0.00001f);

                float worldViewWidth = (float)_graphics.PreferredBackBufferWidth / currentZoom;
                float worldViewHeight = (float)_graphics.PreferredBackBufferHeight / currentZoom;

                float clampMinX = (worldViewWidth / 2f) - CAMERA_BOUNDS_BUFFER;
                float clampMaxX = _mapWidthInPixels - (worldViewWidth / 2f) + CAMERA_BOUNDS_BUFFER;

                float clampMinY = (worldViewHeight / 2f) - CAMERA_BOUNDS_BUFFER;
                float clampMaxY = _mapHeightInPixels - (worldViewHeight / 2f) + CAMERA_BOUNDS_BUFFER;

                _cameraPosition.X = MathHelper.Clamp(_cameraPosition.X, clampMinX, clampMaxX);
                _cameraPosition.Y = MathHelper.Clamp(_cameraPosition.Y, clampMinY, clampMaxY);
            }
        }

        public Matrix GetTransformMatrix()
        {
            Vector2 screenCenter = new Vector2(
                _graphics.PreferredBackBufferWidth / 2f,
                _graphics.PreferredBackBufferHeight / 2f
            );

            return Matrix.CreateTranslation(-_cameraPosition.X, -_cameraPosition.Y, 0f) *
                   Matrix.CreateScale(_zoomScale, _zoomScale, 1f) *
                   Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0f);
        }

        public void UpdateViewport(Viewport viewport)
        {
            _graphics.PreferredBackBufferWidth = viewport.Width;
            _graphics.PreferredBackBufferHeight = viewport.Height;
            _graphics.ApplyChanges();
        }
    }
}