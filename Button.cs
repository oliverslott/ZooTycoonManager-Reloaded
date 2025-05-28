using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Button
{
    private Texture2D backgroundTexture;
    private Texture2D iconTexture;
    private Vector2 position;
    private Rectangle rectangle;
    private Rectangle bounds;
    private SpriteFont font;
    public int GetWidth() => backgroundTexture.Width;
    public int GetHeight() => backgroundTexture.Height;

    public string Text { get; set; }
    public bool IsClicked { get; private set; }

    public Button(Texture2D backgroundTexture, Texture2D iconTexture, Vector2 position, string text = null, SpriteFont font = null)
    {
        this.backgroundTexture = backgroundTexture;
        this.iconTexture = iconTexture;
        this.position = position;
        this.Text = text;
        this.font = font;
        bounds = new Rectangle((int)position.X, (int)position.Y, backgroundTexture.Width, backgroundTexture.Height);
    }
    public void Update(MouseState current, MouseState previous)
    {
        Point mousePosition = current.Position;
        IsClicked = false;

        if (bounds.Contains(mousePosition) &&
            current.LeftButton == ButtonState.Pressed &&
            previous.LeftButton == ButtonState.Released)
        {
            IsClicked = true;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(backgroundTexture, position, Color.White);

        if (iconTexture != null)
        {
            spriteBatch.Draw(iconTexture, position, Color.White);
        }

        if (!string.IsNullOrEmpty(Text) && font != null)
        {
            Vector2 textSize = font.MeasureString(Text);
            Vector2 textPosition = position + new Vector2((backgroundTexture.Width - textSize.X) / 2, (backgroundTexture.Height - textSize.Y) / 2);
            spriteBatch.DrawString(font, Text, textPosition, Color.Black);
        }
    }
    public void SetPosition(Vector2 newPosition)
    {
        position = newPosition;
        bounds = new Rectangle((int)position.X, (int)position.Y, backgroundTexture.Width, backgroundTexture.Height);
    }


}
