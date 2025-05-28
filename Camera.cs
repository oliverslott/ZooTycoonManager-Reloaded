using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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
        public const float CAMERA_BOUNDS_BUFFER = 10 * GameWorld.TILE_SIZE; // Extra space camera can see

        private GraphicsDeviceManager _graphics;

        // Map dimensions for clamping
        private float _mapWidthInPixels;
        private float _mapHeightInPixels;

        private bool _clampToMapBoundaries = true; // Default to true

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
                    // Calculate the minimum zoom required to fit the map within the viewport
                    float minRequiredZoomBasedOnMap = MIN_ZOOM; // Start with the default MIN_ZOOM
                    if (_mapWidthInPixels > 0 && _graphics.PreferredBackBufferWidth > 0)
                    {
                        minRequiredZoomBasedOnMap = Math.Max(minRequiredZoomBasedOnMap, (float)_graphics.PreferredBackBufferWidth / _mapWidthInPixels);
                    }
                    if (_mapHeightInPixels > 0 && _graphics.PreferredBackBufferHeight > 0)
                    {
                        minRequiredZoomBasedOnMap = Math.Max(minRequiredZoomBasedOnMap, (float)_graphics.PreferredBackBufferHeight / _mapHeightInPixels);
                    }

                    // Clamp the zoom scale. If minRequiredZoomBasedOnMap > MAX_ZOOM, zoomScale will become minRequiredZoomBasedOnMap.
                    _zoomScale = MathHelper.Clamp(targetZoom, minRequiredZoomBasedOnMap, MAX_ZOOM);
                }
                else
                {
                    _zoomScale = MathHelper.Clamp(targetZoom, MIN_ZOOM, MAX_ZOOM);
                }
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

            // Clamp camera position to map boundaries
            if (_clampToMapBoundaries && _mapWidthInPixels > 0 && _mapHeightInPixels > 0)
            {
                // Use a very small positive number for zoom if it's zero to prevent division by zero,
                // though previous clamping should prevent _zoomScale from being <= 0.
                float currentZoom = Math.Max(_zoomScale, 0.00001f);

                float worldViewWidth = (float)_graphics.PreferredBackBufferWidth / currentZoom;
                float worldViewHeight = (float)_graphics.PreferredBackBufferHeight / currentZoom;

                // Min/Max camera positions ensure the view stays within map boundaries, plus the buffer.
                // The camera position is the center of the view.
                // Original map corners are (0,0) and (_mapWidthInPixels, _mapHeightInPixels)
                
                float clampMinX = (worldViewWidth / 2f) - CAMERA_BOUNDS_BUFFER;
                float clampMaxX = _mapWidthInPixels - (worldViewWidth / 2f) + CAMERA_BOUNDS_BUFFER;
                
                float clampMinY = (worldViewHeight / 2f) - CAMERA_BOUNDS_BUFFER;
                float clampMaxY = _mapHeightInPixels - (worldViewHeight / 2f) + CAMERA_BOUNDS_BUFFER;

                // If map is smaller than view (e.g., mapWidth < worldViewWidth), clampMaxX could be < clampMinX.
                // In such cases, Clamp will pick clampMinX if _cameraPosition is less, or clampMaxX if greater.
                // However, the zoom clamping logic aims to ensure worldViewWidth <= _mapWidthInPixels + 2 * CAMERA_BOUNDS_BUFFER (effectively).
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