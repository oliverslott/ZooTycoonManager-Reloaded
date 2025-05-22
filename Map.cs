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
                    Tiles[x, y] = new Tile(true, 0); // grass1
                }
            }

            // road logic horizontal
            for (int y = 5; y < Height; y += 10)
            {
                for (int x = 0; x < Width; x++)
                {
                    Tiles[x, y] = new Tile(true, 1); // dirt road
                }
            }

            // road logic vertical
            for (int x = 5; x < Width; x += 10)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tiles[x, y] = new Tile(true, 1); // dirt road
                }
            }
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
