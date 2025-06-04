using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace ZooTycoonManager
{
    public class ShopWindow
    {
        private Texture2D backgroundTexture;
        private List<Button> buttons;
        private Vector2 position;
        private Rectangle backgroundRect;

        public bool IsVisible { get; set; }

        public bool Contains(Vector2 screenPos)
        {
            return backgroundRect.Contains(screenPos);
        }

        public ShopWindow(Texture2D backgroundTexture, Texture2D buttonTexture, SpriteFont font, Vector2 position)
        {
            this.backgroundTexture = backgroundTexture;
            this.position = position;
            IsVisible = false;

            buttons = new List<Button>();

            string[] buttonTexts = { "Buildings", "Habitats", "Animals", "Zookeepers" };

            int padding = 10;
            int spacing = 10;
            int buttonHeight = buttonTexture.Height;
            int buttonWidth = buttonTexture.Width;
            int backgroundWidth = buttonWidth + padding * 2;
            int backgroundHeight = buttonTexts.Length * buttonHeight + (buttonTexts.Length - 1) * spacing + padding * 2;

            backgroundRect = new Rectangle((int)position.X, (int)position.Y, backgroundWidth, backgroundHeight);

            buttons.Clear();
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                Vector2 buttonPosition = new Vector2(position.X + padding, position.Y + padding + i * (buttonHeight + spacing));
                buttons.Add(new Button(buttonTexture, null, buttonPosition, buttonTexts[i], font));
            }
        }

        public void Update(GameTime gameTime, MouseState mouse, MouseState prevMouse)
        {
            if (!IsVisible)
                return;

            foreach (var button in buttons)
            {
                button.Update(mouse, prevMouse);
                if (button.IsClicked)
                {
                    if (button.Text == "Buildings") GameWorld.Instance.ShowSubMenu("Buildings");
                    else if (button.Text == "Habitats") GameWorld.Instance.ShowSubMenu("Habitats");
                    else if (button.Text == "Animals") GameWorld.Instance.ShowSubMenu("Animals");
                    else if (button.Text == "Zookeepers") GameWorld.Instance.ShowSubMenu("Zookeepers");
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            spriteBatch.Draw(backgroundTexture, backgroundRect, Color.White);

            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }

        public void Reposition(Vector2 newPosition)
        {
            position = newPosition;

            int padding = 10;
            int spacing = 10;
            int buttonHeight = buttons[0].GetHeight();
            int buttonWidth = buttons[0].GetWidth();

            int backgroundWidth = buttonWidth + padding * 2;
            int backgroundHeight = buttons.Count * buttonHeight + (buttons.Count - 1) * spacing + padding * 2;

            backgroundRect = new Rectangle((int)position.X, (int)position.Y, backgroundWidth, backgroundHeight);

            for (int i = 0; i < buttons.Count; i++)
            {
                Vector2 newButtonPos = new Vector2(position.X + padding, position.Y + padding + i * (buttonHeight + spacing));
                buttons[i].SetPosition(newButtonPos);
            }
        }
    }
}
