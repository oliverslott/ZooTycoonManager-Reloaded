using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ZooTycoonManager
{
    public class Fence
    {
        private static Texture2D fenceTexture;
        private static HashSet<Vector2> fencePositions = new HashSet<Vector2>();
        private static List<Vector2> fencePositionsList = new List<Vector2>(); // For drawing

        public static void LoadContent(ContentManager content)
        {
            fenceTexture = content.Load<Texture2D>("fence");
        }

        public static void PlaceFence(Vector2 tilePosition)
        {
            // Convert to tile coordinates
            Vector2 tilePos = GameWorld.PixelToTile(tilePosition);
            
            // Check if fence already exists at this position
            Vector2 pixelPos = GameWorld.TileToPixel(tilePos);
            if (fencePositions.Add(pixelPos)) // Add returns true if the item was added
            {
                fencePositionsList.Add(pixelPos);
                // Update walkable map
                GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y] = false;
            }
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (fenceTexture == null) return;

            foreach (Vector2 position in fencePositionsList)
            {
                spriteBatch.Draw(fenceTexture, position, null, Color.White, 0f, 
                    new Vector2(fenceTexture.Width / 2, fenceTexture.Height / 2), 
                    2f, SpriteEffects.None, 0f);
            }
        }
    }
} 