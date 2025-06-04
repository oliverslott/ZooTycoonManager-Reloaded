using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
namespace ZooTycoonManager
{
    public class SubMenuWindow
    {
        private Texture2D backgroundTexture;
        private List<Button> buttons = new List<Button>();
        private Rectangle backgroundRect;
        private Vector2 position;

        public bool IsVisible { get; set; }

        public bool Contains(Vector2 screenPos)
        {
            return backgroundRect.Contains(screenPos);
        }

        public SubMenuWindow(Texture2D backgroundTexture, Texture2D buttonTexture, SpriteFont font, Vector2 position, string[] options)
        {
            this.backgroundTexture = backgroundTexture;
            this.position = position;
            IsVisible = false;

            int padding = 10;
            int spacing = 1;
            int buttonHeight = buttonTexture.Height;
            int buttonWidth = buttonTexture.Width;

            int backgroundWidth = buttonWidth + padding * 2;
            int backgroundHeight = options.Length * buttonHeight + (options.Length - 1) * spacing + padding * 2;

            backgroundRect = new Rectangle((int)position.X, (int)position.Y, backgroundWidth, backgroundHeight);

            for (int i = 0; i < options.Length; i++)
            {
                Vector2 btnPos = new Vector2(position.X + padding, position.Y + padding + i * (buttonHeight + spacing));
                buttons.Add(new Button(buttonTexture, null, btnPos, options[i], font));
            }
        }

        public void Update(MouseState current, MouseState previous)
        {
            if (!IsVisible) return;

            foreach (var button in buttons)
            {
                button.Update(current, previous);
                if (button.IsClicked)
                {
                    if (button.Text == "Small - 5.000")
                    {
                        GameWorld.Instance.StartHabitatPlacement("Small - 5.000");
                    }
                    else if (button.Text == "Medium - 10.000")
                    {
                        GameWorld.Instance.StartHabitatPlacement("Medium - 10.000");
                    }
                    else if (button.Text == "Large - 15.000")
                    {
                        GameWorld.Instance.StartHabitatPlacement("Large - 15.000");
                    }

                    if (button.Text == "Zookeeper - 5.000")
                    {
                        GameWorld.Instance.StartZookeeperPlacement("Zookeeper - 5.000");
                    }
                    else if (button.Text == "Shop - 1.000")
                    {
                        GameWorld.Instance.StartShopPlacement("Shop - 1.000");
                    }
                    else if (button.Text == "Tiles - 10")
                    {
                        GameWorld.Instance.ToggleTilePlacementMode();
                    }
                    else if (button.Text == "Buffalo - 1.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Buffalo - 1.000");
                    }
                    else if (button.Text == "Turtle - 5.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Turtle - 5.000");
                    }
                    else if (button.Text == "Bear - 9.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Bear - 9.000");
                    }
                    else if (button.Text == "Polarbear - 10.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Polarbear - 10.000");
                    }
                    else if (button.Text == "Kangaroo - 2.500")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Kangaroo - 2.500");
                    }
                    else if (button.Text == "Chimpanze - 2.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Chimpanze - 2.000");
                    }
                    else if (button.Text == "Wolf - 4.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Wolf - 4.000");
                    }
                    else if (button.Text == "Camel - 2.500")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Camel - 2.500");
                    }
                    else if (button.Text == "Elephant - 8.000")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Elephant - 8.000");
                    }
                    else if (button.Text == "Orangutan - 2.500")
                    {
                        GameWorld.Instance.StartAnimalPlacement("Orangutan - 2.500");
                    }
                    else if (button.Text == "Tree")
                    {
                        GameWorld.Instance.StartTreePlacement();
                    }
                    else if (button.Text == "Waterhole")
                    {
                        GameWorld.Instance.StartWaterholePlacement();
                    }
                }
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            spriteBatch.Draw(backgroundTexture, backgroundRect, Color.White);
            foreach (var button in buttons)
                button.Draw(spriteBatch);
        }

        public void HideAll() => IsVisible = false;

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
