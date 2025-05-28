using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class Map
    {
        public int Width { get; }
        public int Height { get; }
        public Tile[,] Tiles;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[width, height];
            GenerateTestMap();
        }

        private void GenerateTestMap()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tiles[x, y] = new Tile(false, 0);
                }
            }
            Tiles[GameWorld.VISITOR_SPAWN_TILE_X, GameWorld.VISITOR_SPAWN_TILE_Y] = new Tile(true, 1);
            Tiles[GameWorld.VISITOR_EXIT_TILE_X, GameWorld.VISITOR_EXIT_TILE_Y] = new Tile(true, 1);
        }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            return Tiles[x, y].Walkable;
        }

        public bool[,] ToWalkableArray()
        {
            var result = new bool[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    result[x, y] = Tiles[x, y].Walkable;
            return result;
        }
    }
}
