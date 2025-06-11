using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ZooTycoonManager.Commands
{
    public class PlaceRoadCommand : ICommand
    {
        private readonly Vector2 _tilePosition;
        private int _cost;

        public string Description => $"Place Road at ({_tilePosition.X}, {_tilePosition.Y})";

        public PlaceRoadCommand(Vector2 tilePosition, int cost = 100)
        {
            _tilePosition = tilePosition;
            _cost = cost;
        }

        public bool Execute()
        {
            int x = (int)_tilePosition.X;
            int y = (int)_tilePosition.Y;

            if (GameWorld.Instance.RoadTiles.Contains((x, y)) || !MoneyManager.Instance.SpendMoney(_cost))
            {
                return false;
            }


            if (x < 0 || x >= GameWorld.GRID_WIDTH || y < 0 || y >= GameWorld.GRID_HEIGHT)
            {
                Debug.WriteLine($"Cannot place road: Position ({x}, {y}) is out of bounds.");
                return false;
            }

            // Check if the tile is part of any habitat
            foreach (var habitat in GameWorld.Instance.GetHabitats())
            {
                //if (habitat.ContainsPosition(GameWorld.TileToPixel(_tilePosition)))
                //{
                //    Debug.WriteLine($"Cannot place road: Position ({x}, {y}) is part of an existing habitat.");
                //    return false;
                //}
            }

            GameWorld.Instance.RoadTiles.Add((x, y));

            return true;
        }

        public void Undo()
        {
            int x = (int)_tilePosition.X;
            int y = (int)_tilePosition.Y;

            GameWorld.Instance.RoadTiles.Remove((x, y));
            MoneyManager.Instance.AddMoney(_cost);
        }
    }
}