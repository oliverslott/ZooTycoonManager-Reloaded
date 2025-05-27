using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ZooTycoonManager
{
    public class AnimalInfoPopup
    {
        private SpriteFont _font;
        private Texture2D _backgroundTexture;
        private Texture2D _closeButtonTexture;
        private Rectangle _popupRectangle;
        private Rectangle _closeButtonRectangle;
        private Animal _selectedAnimal;
        private bool _isVisible;
        private GraphicsDevice _graphicsDevice;

        private const int PADDING = 10;
        private const int CLOSE_BUTTON_SIZE = 20;
        private const int PROGRESS_BAR_HEIGHT = 18;
        private const int PROGRESS_BAR_WIDTH = 150;
        private const float ITEM_SPACING = 5f;
        private const float LABEL_BAR_SPACING = 3f;

        public AnimalInfoPopup(GraphicsDevice graphicsDevice, SpriteFont font)
        {
            _graphicsDevice = graphicsDevice;
            _font = font;
            _isVisible = false;

            _backgroundTexture = new Texture2D(graphicsDevice, 1, 1);
            _backgroundTexture.SetData(new[] { Color.White });

            _closeButtonTexture = new Texture2D(graphicsDevice, 1, 1);
            _closeButtonTexture.SetData(new[] { Color.Red });

            _popupRectangle = new Rectangle(
                _graphicsDevice.Viewport.Width - 250 - PADDING, 
                _graphicsDevice.Viewport.Height - 170 - PADDING,
                250, 
                170);

            _closeButtonRectangle = new Rectangle(
                _popupRectangle.X + _popupRectangle.Width - CLOSE_BUTTON_SIZE - PADDING / 2,
                _popupRectangle.Y + PADDING / 2,
                CLOSE_BUTTON_SIZE,
                CLOSE_BUTTON_SIZE);
        }

        public void Show(Animal animal)
        {
            _selectedAnimal = animal;
            _isVisible = true;
            
            _popupRectangle.X = _graphicsDevice.Viewport.Width - _popupRectangle.Width - PADDING;
            _popupRectangle.Y = _graphicsDevice.Viewport.Height - _popupRectangle.Height - PADDING;

            _closeButtonRectangle.X = _popupRectangle.X + _popupRectangle.Width - CLOSE_BUTTON_SIZE - PADDING / 2;
            _closeButtonRectangle.Y = _popupRectangle.Y + PADDING / 2;
        }

        public void Hide()
        {
            _isVisible = false;
            if (_selectedAnimal != null)
            {
                _selectedAnimal.IsSelected = false;
            }
            _selectedAnimal = null;
        }

        public bool IsVisible => _isVisible;

        public bool Update(MouseState mouseState, MouseState prevMouseState)
        {
            if (!_isVisible) return false;

            _popupRectangle.X = _graphicsDevice.Viewport.Width - _popupRectangle.Width - PADDING;
            _popupRectangle.Y = _graphicsDevice.Viewport.Height - _popupRectangle.Height - PADDING;
            _closeButtonRectangle.X = _popupRectangle.X + _popupRectangle.Width - CLOSE_BUTTON_SIZE - PADDING / 2;
            _closeButtonRectangle.Y = _popupRectangle.Y + PADDING / 2;

            if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
            {
                if (_closeButtonRectangle.Contains(mouseState.Position))
                {
                    Hide();
                    return true;
                }
            }
            return false;
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

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible || _selectedAnimal == null) return;

            spriteBatch.Draw(_backgroundTexture, _popupRectangle, Color.DarkSlateGray * 0.8f);
            spriteBatch.Draw(_closeButtonTexture, _closeButtonRectangle, Color.Red);
            spriteBatch.DrawString(_font, "X", new Vector2(_closeButtonRectangle.X + 5, _closeButtonRectangle.Y + 1), Color.White);

            float currentY = _popupRectangle.Y + PADDING;
            currentY = _popupRectangle.Y + PADDING/2 + CLOSE_BUTTON_SIZE + PADDING;

            float leftX = _popupRectangle.X + PADDING;

            string nameText = $"Name: {_selectedAnimal.Name} ({_selectedAnimal.AnimalId})";
            spriteBatch.DrawString(_font, nameText, new Vector2(leftX, currentY), Color.White);
            currentY += _font.LineSpacing + ITEM_SPACING;

            string moodLabelText = "Mood:";
            spriteBatch.DrawString(_font, moodLabelText, new Vector2(leftX, currentY), Color.White);
            currentY += _font.LineSpacing + LABEL_BAR_SPACING;

            float moodPercentage = _selectedAnimal.Mood / 100f;
            Color moodColor = Color.Lerp(Color.Red, Color.LimeGreen, moodPercentage);
            DrawProgressBar(spriteBatch, new Vector2(leftX, currentY), _selectedAnimal.Mood, 100, moodColor);
            currentY += PROGRESS_BAR_HEIGHT + ITEM_SPACING;

            string hungerLabelText = "Hunger:";
            spriteBatch.DrawString(_font, hungerLabelText, new Vector2(leftX, currentY), Color.White);
            currentY += _font.LineSpacing + LABEL_BAR_SPACING;
            
            float displayedHunger = 100f - _selectedAnimal.Hunger;
            float invertedHungerPercentage = displayedHunger / 100f; 
            Color hungerColor = Color.Lerp(Color.Red, Color.LimeGreen, invertedHungerPercentage); 
            DrawProgressBar(spriteBatch, new Vector2(leftX, currentY), displayedHunger, 100, hungerColor);
        }
    }
} 