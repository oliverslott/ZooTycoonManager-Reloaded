using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ZooTycoonManager.Commands
{
    public class PlaceRoadCommand : ICommand
    {
        private readonly Vector2 _tilePosition;
        private Tile _originalTile;
        private const int ROAD_TEXTURE_INDEX = 1;
        private int _cost;

        public string Description => $"Place Road at ({_tilePosition.X}, {_tilePosition.Y})";

        public PlaceRoadCommand(Vector2 tilePosition, Tile originalTileFromGameWorld, int cost = 100)
        {
            _tilePosition = tilePosition;
            _originalTile = new Tile(originalTileFromGameWorld.Walkable, originalTileFromGameWorld.TextureIndex);
            _cost = cost;
        }

        public bool Execute()
        {
            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"PlaceShopCommand: Could not spend {_cost} for shop. Current balance: {MoneyManager.Instance.CurrentMoney}. Placement failed.");
                return false;
            }

            int x = (int)_tilePosition.X;
            int y = (int)_tilePosition.Y;

            if (x < 0 || x >= GameWorld.GRID_WIDTH || y < 0 || y >= GameWorld.GRID_HEIGHT)
            {
                Debug.WriteLine($"Cannot place road: Position ({x}, {y}) is out of bounds.");
                return false;
            }

            // Check if the tile is part of any habitat
            foreach (var habitat in GameWorld.Instance.GetHabitats())
            {
                if (habitat.ContainsPosition(GameWorld.TileToPixel(_tilePosition)))
                {
                    Debug.WriteLine($"Cannot place road: Position ({x}, {y}) is part of an existing habitat.");
                    return false;
                }
            }

            GameWorld.Instance.UpdateTile(x, y, true, ROAD_TEXTURE_INDEX);

            Debug.WriteLine($"Executed: Placed road tile at ({x}, {y}). Original was Walkable: {_originalTile.Walkable}, Texture: {_originalTile.TextureIndex}");
            return true;
        }

        public void Undo()
        {
            int x = (int)_tilePosition.X;
            int y = (int)_tilePosition.Y;

            if (x < 0 || x >= GameWorld.GRID_WIDTH || y < 0 || y >= GameWorld.GRID_HEIGHT)
            {
                Debug.WriteLine($"Cannot undo road placement: Position ({x}, {y}) is out of bounds.");
                return;
            }

            if (_originalTile == null)
            {
                Debug.WriteLine($"Cannot undo road placement at ({x}, {y}): Original tile state is null.");
                return;
            }

            MoneyManager.Instance.AddMoney(_cost);

            GameWorld.Instance.UpdateTile(x, y, _originalTile.Walkable, _originalTile.TextureIndex);
            Debug.WriteLine($"Undid: Restored tile at ({x}, {y}) to Walkable: {_originalTile.Walkable}, Texture: {_originalTile.TextureIndex}");
        }
    }
} 