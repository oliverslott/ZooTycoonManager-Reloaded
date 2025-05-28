using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class TileRenderer
    {
        private Texture2D[] tileTextures;
        private int tileSize;

        public TileRenderer(Texture2D[] tileTextures, int tileSize = 32)
        {
            this.tileTextures = tileTextures;
            this.tileSize = tileSize;
        }

        public void Draw(SpriteBatch spriteBatch, Map map)
        {
            // Calculate the extended drawing boundaries
            int bufferTiles = (int)(Camera.CAMERA_BOUNDS_BUFFER / tileSize);
            int startX = -bufferTiles;
            int endX = map.Width + bufferTiles;
            int startY = -bufferTiles;
            int endY = map.Height + bufferTiles;

            Texture2D grassTexture = tileTextures[0]; // Assuming grass is always at index 0

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    // If within the original map boundaries, draw the actual map tile
                    if (x >= 0 && x < map.Width && y >= 0 && y < map.Height)
                    {
                        Tile tile = map.Tiles[x, y];
                        Texture2D texture = tileTextures[tile.TextureIndex];
                        spriteBatch.Draw(
                            texture,
                            new Vector2(x * tileSize, y * tileSize),
                            Color.White
                        );
                    }
                    else // Otherwise, if outside the map, draw a grass tile
                    {
                        spriteBatch.Draw(
                            grassTexture,
                            new Vector2(x * tileSize, y * tileSize),
                            Color.White
                        );
                    }
                }
            }
        }
    }
}
