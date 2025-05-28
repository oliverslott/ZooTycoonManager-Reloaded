using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class PlaceRoadCommand : ICommand
    {
        private readonly Vector2 _tilePosition;
        private Tile _originalTile;
        private const int ROAD_TEXTURE_INDEX = 1;

        public string Description => $"Place Road at ({_tilePosition.X}, {_tilePosition.Y})";

        public PlaceRoadCommand(Vector2 tilePosition, Tile originalTileFromGameWorld)
        {
            _tilePosition = tilePosition;
            _originalTile = new Tile(originalTileFromGameWorld.Walkable, originalTileFromGameWorld.TextureIndex);
        }

        public bool Execute()
        {
            int x = (int)_tilePosition.X;
            int y = (int)_tilePosition.Y;

            if (x < 0 || x >= GameWorld.GRID_WIDTH || y < 0 || y >= GameWorld.GRID_HEIGHT)
            {
                Debug.WriteLine($"Cannot place road: Position ({x}, {y}) is out of bounds.");
                return false;
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


            GameWorld.Instance.UpdateTile(x, y, _originalTile.Walkable, _originalTile.TextureIndex);
            Debug.WriteLine($"Undid: Restored tile at ({x}, {y}) to Walkable: {_originalTile.Walkable}, Texture: {_originalTile.TextureIndex}");
        }
    }
} 