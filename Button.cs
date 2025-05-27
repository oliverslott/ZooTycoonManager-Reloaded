using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Button
{
    private Texture2D backgroundTexture;
    private Texture2D iconTexture;
    private Vector2 position;
    private Rectangle rectangle;

    public bool IsClicked { get; private set; }

    public Button(Texture2D backgroundTexture, Texture2D iconTexture, Vector2 position)
    {
        this.backgroundTexture = backgroundTexture;
        this.iconTexture = iconTexture;
        this.position = position;
        rectangle = new Rectangle((int)position.X, (int)position.Y, backgroundTexture.Width, backgroundTexture.Height);
    }

    public void Update(MouseState mouseState, MouseState prevMouseState)
    {
        Rectangle mouseRect = new Rectangle(mouseState.X, mouseState.Y, 1, 1);

        if (mouseRect.Intersects(rectangle) && mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
        {
            IsClicked = true;
        }
        else
        {
            IsClicked = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(backgroundTexture, position, Color.White);
        spriteBatch.Draw(iconTexture, position, Color.White);
    }
}
