using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZooTycoonManager.Commands
{
    public class PlaceShopCommand : ICommand
    {
        private Vector2 _position;
        private int _widthInTiles;
        private int _heightInTiles;
        private Shop _placedShop;
        private int _cost;

        public PlaceShopCommand(Vector2 position, int widthInTiles, int heightInTiles, int cost = 0)
        {
            _position = position;
            _widthInTiles = widthInTiles;
            _heightInTiles = heightInTiles;
            _cost = cost;
        }

        public bool Execute()
        {
            Vector2 cursorTile = GameWorld.PixelToTile(_position);

            int shopTopLeftTileX = (int)cursorTile.X - (_widthInTiles / 2);
            int shopTopLeftTileY = (int)cursorTile.Y - (_heightInTiles / 2);

            Vector2 snappedPixelPosition = new Vector2(
                shopTopLeftTileX * GameWorld.TILE_SIZE,
                shopTopLeftTileY * GameWorld.TILE_SIZE
            );

            Vector2 startTile = GameWorld.PixelToTile(snappedPixelPosition);

            HashSet<Vector2> newShopTiles = new HashSet<Vector2>();
            for (int x = 0; x < _widthInTiles; x++)
            {
                for (int y = 0; y < _heightInTiles; y++)
                {
                    newShopTiles.Add(new Vector2(startTile.X + x, startTile.Y + y));
                }
            }

            foreach (Vector2 tile in newShopTiles)
            {
                int currentTileX = (int)tile.X;
                int currentTileY = (int)tile.Y;

                if (currentTileX < 0 || currentTileX >= GameWorld.GRID_WIDTH ||
                    currentTileY < 0 || currentTileY >= GameWorld.GRID_HEIGHT)
                {
                    Debug.WriteLine($"PlaceShopCommand: Attempted to place shop out of bounds at tile ({currentTileX}, {currentTileY}). Placement failed.");
                    return false;
                }

                foreach (var existingShop in GameWorld.Instance.GetShops())
                {
                    Rectangle existingShopTileBounds = new Rectangle(existingShop.PositionX, existingShop.PositionY, existingShop.WidthInTiles, existingShop.HeightInTiles);
                    if (existingShopTileBounds.Contains(currentTileX, currentTileY))
                    {
                        Debug.WriteLine($"PlaceShopCommand: Attempted to place shop on an existing shop at tile ({currentTileX}, {currentTileY}). Placement failed.");
                        return false;
                    }
                }

                foreach (var habitat in GameWorld.Instance.GetHabitats())
                {
                    if (habitat.GetFencePositions().Contains(tile))
                    {
                        Debug.WriteLine($"PlaceShopCommand: Attempted to place shop on a habitat fence at tile ({currentTileX}, {currentTileY}). Placement failed.");
                        return false;
                    }
                    Vector2 tileCenterPixel = GameWorld.TileToPixel(tile);
                    if (habitat.ContainsPosition(tileCenterPixel))
                    {
                        Debug.WriteLine($"PlaceShopCommand: Attempted to place shop inside a habitat area at tile ({currentTileX}, {currentTileY}). Placement failed.");
                        return false;
                    }
                }

                if (GameWorld.Instance.map.Tiles[currentTileX, currentTileY].TextureIndex == GameWorld.ROAD_TEXTURE_INDEX)
                {
                    Debug.WriteLine($"PlaceShopCommand: Attempted to place shop on a road tile ({currentTileX}, {currentTileY}). Placement failed.");
                    return false;
                }
            }


            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"PlaceShopCommand: Could not spend {_cost} for shop. Current balance: {MoneyManager.Instance.CurrentMoney}. Placement failed.");
                return false;
            }

            _placedShop = new Shop(snappedPixelPosition, _widthInTiles, _heightInTiles, GameWorld.Instance.GetNextShopId(), _cost);
            _placedShop.LoadContent(GameWorld.Instance.Content);
            
            GameWorld.Instance.GetShops().Add(_placedShop);

            Debug.WriteLine($"Executed PlaceShopCommand: Shop {_placedShop.ShopId} at {snappedPixelPosition}, Cost: {_cost}");
            return true;
        }

        public void Undo()
        {
            if (_placedShop != null)
            {
                GameWorld.Instance.GetShops().Remove(_placedShop);

                Vector2 startTile = GameWorld.PixelToTile(_placedShop.Position);
                for (int x = 0; x < _placedShop.WidthInPixels / GameWorld.TILE_SIZE; x++)
                {
                    for (int y = 0; y < _placedShop.HeightInPixels / GameWorld.TILE_SIZE; y++)
                    {
                        int tileX = (int)startTile.X + x;
                        int tileY = (int)startTile.Y + y;
                        if (tileX >= 0 && tileX < GameWorld.GRID_WIDTH && tileY >= 0 && tileY < GameWorld.GRID_HEIGHT)
                        {
                            GameWorld.Instance.WalkableMap[tileX, tileY] = GameWorld.Instance.GetOriginalWalkableState(tileX, tileY);
                        }
                    }
                }

                MoneyManager.Instance.AddMoney(_cost);
                Debug.WriteLine($"Undone PlaceShopCommand: Shop {_placedShop.ShopId} removed. Cost {_cost} refunded.");
                _placedShop = null;
            }
        }

        public string Description => $"Place Shop at ({_position.X / GameWorld.TILE_SIZE}, {_position.Y / GameWorld.TILE_SIZE}) for ${_cost}";
    }
} 