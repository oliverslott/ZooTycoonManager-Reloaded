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

        public bool IsVisible { get; set; }

        public ShopWindow(Texture2D backgroundTexture, Texture2D buttonTexture, SpriteFont font, Vector2 position)
        {
            this.backgroundTexture = backgroundTexture;
            this.position = position;
            IsVisible = false;

            buttons = new List<Button>();

            string[] buttonTexts = { "Buildings", "Habitats", "Animals", "Zookeepers" };

            // Placer knapperne pænt
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                Vector2 buttonPosition = position + new Vector2(10, 10 + i * (buttonTexture.Height + 10));
                buttons.Add(new Button(buttonTexture, null, buttonPosition, buttonTexts[i], font));
            }
        }

        public void Update(GameTime gameTime, MouseState mouseState)
        {
            if (!IsVisible)
                return;

            foreach (var button in buttons)
            {
                button.Update(mouseState);
                if (button.IsClicked)
                {
                    Console.WriteLine($"Clicked {button.Text}");
                    // Du kan tilføje logik her for at åbne under-menuer
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            spriteBatch.Draw(backgroundTexture, position, Color.White);

            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }
    }
}
