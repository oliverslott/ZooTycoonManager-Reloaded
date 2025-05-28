using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class PlaceRoadCommand : ICommand
    {
        private readonly Vector2 _tilePosition;
        private Tile _originalTile; // To store the tile's state before modification
        private const int ROAD_TEXTURE_INDEX = 1; // Assuming 1 is the texture index for dirt/road

        public string Description => $"Place Road at ({_tilePosition.X}, {_tilePosition.Y})";

        public PlaceRoadCommand(Vector2 tilePosition, Tile originalTileFromGameWorld)
        {
            _tilePosition = tilePosition;
            // Create a new Tile object to store a snapshot of the original state
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

            // Store the original tile state if not already (though constructor should handle this)
            // This is more of a safeguard or if Execute could be called multiple times before Undo.
            // For this command, it's set in constructor, so direct modification is fine.

            // Check if already a road to prevent redundant commands / state changes.
            // Though, CommandManager handles undo/redo, so maybe not strictly necessary
            // if the visual/walkable state doesn't change. For now, let's assume we always want to execute.

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

            // Restore the original tile
            GameWorld.Instance.UpdateTile(x, y, _originalTile.Walkable, _originalTile.TextureIndex);
            Debug.WriteLine($"Undid: Restored tile at ({x}, {y}) to Walkable: {_originalTile.Walkable}, Texture: {_originalTile.TextureIndex}");
        }
    }
} 