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
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Tile tile = map.Tiles[x, y];
                    Texture2D texture = tileTextures[tile.TextureIndex];

                    spriteBatch.Draw(
                        texture,
                        new Vector2(x * tileSize, y * tileSize),
                        Color.White
                    );
                }
            }
        }
    }
}
