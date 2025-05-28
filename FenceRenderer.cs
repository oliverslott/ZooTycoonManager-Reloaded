using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public static class FenceRenderer
    {
        private static Texture2D fenceTexture;
        private static Texture2D fenceConnectedBottomTexture;
        private static Texture2D fenceConnectedLeftBottomTexture;
        private static Texture2D fenceConnectedRightBottomTexture;
        private static Texture2D fenceConnectedLeftRightBottomTexture;
        private static Texture2D fenceConnectedLeftTexture;
        private static Texture2D fenceConnectedRightTexture;
        private static Texture2D fenceConnectedLeftRightTexture;

        public static void LoadContent(ContentManager content)
        {
            fenceTexture = content.Load<Texture2D>("fence");
            fenceConnectedBottomTexture = content.Load<Texture2D>("fence-connected-bottom");
            fenceConnectedLeftBottomTexture = content.Load<Texture2D>("fence-connected-left-bottom");
            fenceConnectedRightBottomTexture = content.Load<Texture2D>("fence-connected-right-bottom");
            fenceConnectedLeftRightBottomTexture = content.Load<Texture2D>("fence-connected-left-right-bottom");
            fenceConnectedLeftTexture = content.Load<Texture2D>("fence-connected-left");
            fenceConnectedRightTexture = content.Load<Texture2D>("fence-connected-right");
            fenceConnectedLeftRightTexture = content.Load<Texture2D>("fence-connected-left-right");
        }

        public static void Draw(SpriteBatch spriteBatch, IEnumerable<Vector2> fenceTilePositions, HashSet<Vector2> fenceTileCoordinates, float scale)
        {
            if (fenceTexture == null)
            {
                Debug.WriteLine("FenceRenderer.Draw called before textures were loaded or textures are missing.");
                return;
            }

            foreach (Vector2 tilePos in fenceTilePositions)
            {
                bool hasLeft = fenceTileCoordinates.Contains(new Vector2(tilePos.X - 1, tilePos.Y));
                bool hasRight = fenceTileCoordinates.Contains(new Vector2(tilePos.X + 1, tilePos.Y));
                bool hasBottom = fenceTileCoordinates.Contains(new Vector2(tilePos.X, tilePos.Y + 1));

                Texture2D currentFenceTexture = fenceTexture;

                if (hasLeft && hasRight && hasBottom)
                {
                    currentFenceTexture = fenceConnectedLeftRightBottomTexture;
                }
                else if (hasLeft && hasRight)
                {
                    currentFenceTexture = fenceConnectedLeftRightTexture;
                }
                else if (hasLeft && hasBottom)
                {
                    currentFenceTexture = fenceConnectedLeftBottomTexture;
                }
                else if (hasRight && hasBottom)
                {
                    currentFenceTexture = fenceConnectedRightBottomTexture;
                }
                else if (hasLeft)
                {
                    currentFenceTexture = fenceConnectedLeftTexture;
                }
                else if (hasRight)
                {
                    currentFenceTexture = fenceConnectedRightTexture;
                }
                else if (hasBottom)
                {
                    currentFenceTexture = fenceConnectedBottomTexture;
                }

                Vector2 pixelDrawPosition = GameWorld.TileToPixel(tilePos);

                spriteBatch.Draw(currentFenceTexture, pixelDrawPosition, null, Color.White, 0f,
                    new Vector2(currentFenceTexture.Width / 2, currentFenceTexture.Height / 2),
                    scale, SpriteEffects.None, 0f);
            }
        }
    }
} 