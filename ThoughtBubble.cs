using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ZooTycoonManager
{
    public class ThoughtBubble
    {
        private Texture2D _thoughtBubbleTexture;

        public ThoughtBubble()
        {
        }

        public void LoadContent(ContentManager contentManager)
        {
            _thoughtBubbleTexture = contentManager.Load<Texture2D>("Thought_bubble");
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 parentPosition, float parentSpriteHeight, Texture2D contentTexture, Rectangle? sourceRectangleForContent = null, float contentScale = 1f)
        {
            if (_thoughtBubbleTexture == null || contentTexture == null) return;

            Vector2 thoughtBubblePosition = new Vector2(parentPosition.X, parentPosition.Y - parentSpriteHeight); 

            spriteBatch.Draw(_thoughtBubbleTexture, thoughtBubblePosition, null, Color.White, 0f, new Vector2(_thoughtBubbleTexture.Width / 2, _thoughtBubbleTexture.Height /2), 0.5f, SpriteEffects.None, 0.1f);

            Vector2 contentTexturePosition = new Vector2(thoughtBubblePosition.X, thoughtBubblePosition.Y - 4);

            Rectangle finalSourceRect;
            Vector2 origin;

            if (sourceRectangleForContent.HasValue)
            {
                finalSourceRect = sourceRectangleForContent.Value;
                origin = new Vector2(finalSourceRect.Width / 2f, finalSourceRect.Height / 2f);
            }
            else
            {
                finalSourceRect = new Rectangle(0, 0, contentTexture.Width, contentTexture.Height);
                origin = new Vector2(contentTexture.Width / 2f, contentTexture.Height / 2f);
            }

            spriteBatch.Draw(contentTexture, contentTexturePosition, finalSourceRect, Color.White, 0f, origin, contentScale, SpriteEffects.None, 0.2f);
        }
    }
} 