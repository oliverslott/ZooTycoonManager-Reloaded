using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Taskbar;

namespace ZooTycoonManager
{
    public class SaveButton
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SaveButton saveButton;
        private SpriteFont font;
        private Texture2D backgroundTexture;
        private List<Button> buttons;
        private Vector2 position;
        private Rectangle backgroundRect;
        private List<Habitat> habitats;


        public SaveButton(Texture2D buttonTexture, SpriteFont font, Vector2 position)
        {
            this.position = position;

            buttons = new List<Button>();

            string[] buttonTexts = { "Save"};

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
        

        public void Update(GameTime gametime, MouseState current, MouseState previous)
        {
            foreach (var button in buttons)
            {
                button.Update(current, previous);

                if (button.IsClicked)
                {
                    SaveGame();
                }
            }

            
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }

        private void SaveGame()
        {
            DatabaseManager.Instance.SaveGame();
            Debug.WriteLine("Save Game");
        }
    }
}
