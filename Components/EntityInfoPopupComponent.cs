using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using ZooTycoonManager.Interfaces;

namespace ZooTycoonManager.Components
{
    public class EntityInfoPopupComponent : Component
    {
        private SpriteFont _font;
        private Texture2D _backgroundTexture;
        private Texture2D _closeButtonTexture;
        private Rectangle _popupRectangle;
        private Rectangle _closeButtonRectangle;
        private IInspectableEntity _selectedEntity;
        private bool _isVisible;
        private GraphicsDevice _graphicsDevice;

        private const int PADDING = 10;
        private const int CLOSE_BUTTON_SIZE = 20;
        private const int PROGRESS_BAR_HEIGHT = 18;
        private const int PROGRESS_BAR_WIDTH = 150;
        private const float ITEM_SPACING = 5f;
        private const float LABEL_BAR_SPACING = 3f;
        
        private MouseState prevMouseState;

        public bool PopupHandledClick { get; private set; }

        public override void Initialize()
        {
            _graphicsDevice = GameWorld.Instance.GraphicsDevice;
            _isVisible = false;

            const int popupWidth = 270;
            const int popupHeight = 226;

            _popupRectangle = new Rectangle(
                _graphicsDevice.Viewport.Width - popupWidth - PADDING,
                _graphicsDevice.Viewport.Height - popupHeight - PADDING,
                popupWidth,
                popupHeight);

            _closeButtonRectangle = new Rectangle(
                _popupRectangle.X + _popupRectangle.Width - CLOSE_BUTTON_SIZE - PADDING / 2,
                _popupRectangle.Y + PADDING / 2,
                CLOSE_BUTTON_SIZE,
                CLOSE_BUTTON_SIZE);
        }

        public override void LoadContent(ContentManager contentManager)
        {
            _font = contentManager.Load<SpriteFont>("font");
            _backgroundTexture = new Texture2D(_graphicsDevice, 1, 1);
            _backgroundTexture.SetData(new[] { Color.White });

            _closeButtonTexture = new Texture2D(_graphicsDevice, 1, 1);
            _closeButtonTexture.SetData(new[] { Color.Red });
        }

        public void Show(IInspectableEntity entity)
        {
            _selectedEntity = entity;
            _isVisible = true;

            _popupRectangle.X = _graphicsDevice.Viewport.Width - _popupRectangle.Width - PADDING;
            _popupRectangle.Y = _graphicsDevice.Viewport.Height - _popupRectangle.Height - PADDING;

            _closeButtonRectangle.X = _popupRectangle.X + _popupRectangle.Width - CLOSE_BUTTON_SIZE - PADDING / 2;
            _closeButtonRectangle.Y = _popupRectangle.Y + PADDING / 2;
            Owner.IsActive = true;
        }

        public void Hide()
        {
            _isVisible = false;
            if (_selectedEntity != null)
            {
                _selectedEntity.IsSelected = false;
            }
            _selectedEntity = null;
            Owner.IsActive = false;
        }
        
        public bool IsVisible => _isVisible;

        public override void Update(GameTime gameTime)
        {
            if (!_isVisible)
            {
                PopupHandledClick = false;
                return;
            }

            MouseState mouseState = Mouse.GetState();

            _popupRectangle.X = _graphicsDevice.Viewport.Width - _popupRectangle.Width - PADDING;
            _popupRectangle.Y = _graphicsDevice.Viewport.Height - _popupRectangle.Height - PADDING;
            _closeButtonRectangle.X = _popupRectangle.X + _popupRectangle.Width - CLOSE_BUTTON_SIZE - PADDING / 2;
            _closeButtonRectangle.Y = _popupRectangle.Y + PADDING / 2;
            
            PopupHandledClick = false;
            if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
            {
                if (_popupRectangle.Contains(mouseState.Position))
                {
                    PopupHandledClick = true;
                }

                if (_closeButtonRectangle.Contains(mouseState.Position))
                {
                    Hide();
                }
            }

            prevMouseState = mouseState;
        }

        private void DrawProgressBar(SpriteBatch spriteBatch, Vector2 barDrawPosition, float currentValue, float maxValue, Color fillColor)
        {
            Rectangle barBackgroundRect = new Rectangle((int)barDrawPosition.X, (int)barDrawPosition.Y, PROGRESS_BAR_WIDTH, PROGRESS_BAR_HEIGHT);
            spriteBatch.Draw(_backgroundTexture, barBackgroundRect, Color.Gray);

            float percentage = Math.Clamp(currentValue / maxValue, 0f, 1f);
            int fillWidth = (int)(PROGRESS_BAR_WIDTH * percentage);
            Rectangle barFillRect = new Rectangle((int)barDrawPosition.X, (int)barDrawPosition.Y, fillWidth, PROGRESS_BAR_HEIGHT);
            spriteBatch.Draw(_backgroundTexture, barFillRect, fillColor);

            string valueText = $"{currentValue:0}/{maxValue:0}";
            Vector2 valueTextSize = _font.MeasureString(valueText);
            float textY = barDrawPosition.Y + (PROGRESS_BAR_HEIGHT - valueTextSize.Y) / 2;
            spriteBatch.DrawString(_font, valueText, new Vector2(barDrawPosition.X + PROGRESS_BAR_WIDTH + 5, textY), Color.White);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible || _selectedEntity == null) return;

            spriteBatch.Draw(_backgroundTexture, _popupRectangle, Color.DarkSlateGray * 0.8f);
            spriteBatch.Draw(_closeButtonTexture, _closeButtonRectangle, Color.Red);
            spriteBatch.DrawString(_font, "X", new Vector2(_closeButtonRectangle.X + 5, _closeButtonRectangle.Y + 1), Color.White);

            float currentY = _popupRectangle.Y + PADDING;
            currentY = _popupRectangle.Y + PADDING / 2 + CLOSE_BUTTON_SIZE + PADDING;

            float leftX = _popupRectangle.X + PADDING;

            string nameText = $"Name: {_selectedEntity.Name} ({_selectedEntity.Id})";
            spriteBatch.DrawString(_font, nameText, new Vector2(leftX, currentY), Color.White);
            currentY += _font.LineSpacing + ITEM_SPACING;

            string moodLabelText = "Mood:";
            spriteBatch.DrawString(_font, moodLabelText, new Vector2(leftX, currentY), Color.White);
            currentY += _font.LineSpacing + LABEL_BAR_SPACING;

            float moodPercentage = _selectedEntity.Mood / 100f;
            Color moodColor = Color.Lerp(Color.Red, Color.LimeGreen, moodPercentage);
            DrawProgressBar(spriteBatch, new Vector2(leftX, currentY), _selectedEntity.Mood, 100, moodColor);
            currentY += PROGRESS_BAR_HEIGHT + ITEM_SPACING;

            string hungerLabelText = "Hunger:";
            spriteBatch.DrawString(_font, hungerLabelText, new Vector2(leftX, currentY), Color.White);
            currentY += _font.LineSpacing + LABEL_BAR_SPACING;

            float displayedHunger = 100f - _selectedEntity.Hunger;
            float invertedHungerPercentage = displayedHunger / 100f;
            Color hungerColor = Color.Lerp(Color.Red, Color.LimeGreen, invertedHungerPercentage);
            DrawProgressBar(spriteBatch, new Vector2(leftX, currentY), displayedHunger, 100, hungerColor);
            currentY += PROGRESS_BAR_HEIGHT + ITEM_SPACING;

            if (_selectedEntity is IStressableEntity stressableEntity)
            {
                string stressLabelText = "Stress:";
                spriteBatch.DrawString(_font, stressLabelText, new Vector2(leftX, currentY), Color.White);
                currentY += _font.LineSpacing + LABEL_BAR_SPACING;

                float displayedStress = 100f - stressableEntity.Stress;
                float displayedStressPercentage = displayedStress / 100f;

                Color stressColor = Color.Lerp(Color.Red, Color.LimeGreen, displayedStressPercentage);
                DrawProgressBar(spriteBatch, new Vector2(leftX, currentY), displayedStress, 100, stressColor);
            }
        }
    }
} 