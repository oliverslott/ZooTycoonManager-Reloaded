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
                    Tiles[x, y] = new Tile(0);
                }
            }
            Tiles[GameWorld.VISITOR_SPAWN_TILE_X, GameWorld.VISITOR_SPAWN_TILE_Y] = new Tile(1);
            Tiles[GameWorld.VISITOR_EXIT_TILE_X, GameWorld.VISITOR_EXIT_TILE_Y] = new Tile(1);
        }
    }
}
