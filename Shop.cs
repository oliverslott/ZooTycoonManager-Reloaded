using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Microsoft.Data.Sqlite;
using System;

namespace ZooTycoonManager
{
    public class Shop : ISaveable, ILoadable
    {
        private Texture2D _texture;
        private Vector2 _position;
        private int _widthInTiles;
        private int _heightInTiles;
        private Rectangle _bounds;
        private HashSet<Vector2> _tileCoordinates;

        private const int MAX_VISITORS = 2;
        private SemaphoreSlim _visitorSemaphore;
        private HashSet<Visitor> _currentVisitors;

        public int ShopId { get; private set; }
        public string Type { get; set; }
        public int Cost { get; set; }

        public Vector2 Position
        {
            get => _position;
            private set
            {
                _position = value;
                Vector2 tilePos = GameWorld.PixelToTile(value);
                PositionX = (int)tilePos.X;
                PositionY = (int)tilePos.Y;
            }
        }

        public int WidthInTiles => _widthInTiles;
        public int HeightInTiles => _heightInTiles;

        public int PositionX { get; private set; }
        public int PositionY { get; private set; }
        public int WidthInPixels => _widthInTiles * GameWorld.TILE_SIZE;
        public int HeightInPixels => _heightInTiles * GameWorld.TILE_SIZE;

        public Shop(Vector2 worldPosition, int widthInTiles, int heightInTiles, int shopId, int cost)
        {
            ShopId = shopId;
            _widthInTiles = widthInTiles;
            _heightInTiles = heightInTiles;
            Type = "Food";
            Cost = cost;
            Position = SnapToTile(worldPosition);
            InitializeShopState();
        }

        public Shop()
        {
            _widthInTiles = 3;
            _heightInTiles = 3;
            Type = "Food";
            Cost = 0;
        }

        private void InitializeShopState()
        {
            _bounds = new Rectangle(
                (int)_position.X,
                (int)_position.Y,
                WidthInPixels,
                HeightInPixels
            );

            _tileCoordinates = new HashSet<Vector2>();
            Vector2 startTile = GameWorld.PixelToTile(_position);
            for (int x = 0; x < _widthInTiles; x++)
            {
                for (int y = 0; y < _heightInTiles; y++)
                {
                    Vector2 currentTile = new Vector2(startTile.X + x, startTile.Y + y);
                    _tileCoordinates.Add(currentTile);
                    if (GameWorld.Instance != null && GameWorld.Instance.WalkableMap != null)
                    {
                        if (currentTile.X >= 0 && currentTile.X < GameWorld.GRID_WIDTH &&
                            currentTile.Y >= 0 && currentTile.Y < GameWorld.GRID_HEIGHT)
                        {
                            GameWorld.Instance.WalkableMap[(int)currentTile.X, (int)currentTile.Y] = false;
                        }
                    }
                    else
                    {
                        Debug.WriteLineIf(GameWorld.Instance == null, "GameWorld.Instance is null during Shop tile initialization.");
                        Debug.WriteLineIf(GameWorld.Instance != null && GameWorld.Instance.WalkableMap == null, "GameWorld.Instance.WalkableMap is null during Shop tile initialization.");
                    }
                }
            }

            _visitorSemaphore = new SemaphoreSlim(MAX_VISITORS);
            _currentVisitors = new HashSet<Visitor>();
        }

        private Vector2 SnapToTile(Vector2 pixelPosition)
        {
            Vector2 tile = GameWorld.PixelToTile(pixelPosition);
            return new Vector2(tile.X * GameWorld.TILE_SIZE, tile.Y * GameWorld.TILE_SIZE);
        }

        public void LoadContent(ContentManager content)
        {
            _texture = content.Load<Texture2D>("foodshopsprite_cut");
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_texture != null)
            {
                spriteBatch.Draw(_texture, _bounds, Color.White);
            }
        }

        public void Update(GameTime gameTime)
        {
        }

        public List<Vector2> GetWalkableVisitingSpots()
        {
            List<Vector2> visitingSpots = new List<Vector2>();
            Vector2 startTile = GameWorld.PixelToTile(_position);

            for (int x = -1; x <= _widthInTiles; x++)
            {
                for (int y = -1; y <= _heightInTiles; y++)
                {
                    if (x == -1 || x == _widthInTiles || y == -1 || y == _heightInTiles)
                    {
                        int checkX = (int)startTile.X + x;
                        int checkY = (int)startTile.Y + y;

                        if (checkX >= 0 && checkX < GameWorld.GRID_WIDTH &&
                            checkY >= 0 && checkY < GameWorld.GRID_HEIGHT)
                        {
                            Vector2 adjacentTile = new Vector2(checkX, checkY);
                            if (GameWorld.Instance.WalkableMap[checkX, checkY] && !_tileCoordinates.Contains(adjacentTile))
                            {
                                visitingSpots.Add(adjacentTile);
                            }
                        }
                    }
                }
            }
            return visitingSpots.Distinct().ToList();
        }

        public bool TryEnterShopSync(Visitor visitor)
        {
            if (_visitorSemaphore.Wait(0))
            {
                lock (_currentVisitors)
                {
                    _currentVisitors.Add(visitor);
                }
                Debug.WriteLine($"Visitor {visitor.VisitorId} entered Shop {ShopId}. Current shop visitors: {_currentVisitors.Count}");
                return true;
            }
            Debug.WriteLine($"Visitor {visitor.VisitorId} failed to enter Shop {ShopId} (full). Current shop visitors: {_currentVisitors.Count}");
            return false;
        }

        public void LeaveShop(Visitor visitor)
        {
            lock (_currentVisitors)
            {
                if (_currentVisitors.Remove(visitor))
                {
                    _visitorSemaphore.Release();
                    Debug.WriteLine($"Visitor {visitor.VisitorId} left Shop {ShopId}. Current shop visitors: {_currentVisitors.Count}");
                }
            }
        }

        public void VisitorInteraction(Visitor visitor)
        {
            visitor.Hunger = 0;
            MoneyManager.Instance.AddMoney(10);
            Debug.WriteLine($"Visitor {visitor.VisitorId} visited Shop {ShopId} and hunger is now 0.");
        }

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$shop_id", ShopId);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);
            command.Parameters.AddWithValue("$type", (object)Type ?? DBNull.Value);
            command.Parameters.AddWithValue("$cost", (object)Cost ?? DBNull.Value);

            try
            {
                command.CommandText = @"
                    INSERT INTO Shop (shop_id, position_x, position_y, type, cost)
                    VALUES ($shop_id, $position_x, $position_y, $type, $cost);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Shop: ID {ShopId}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                command.CommandText = @"
                    UPDATE Shop 
                    SET position_x = $position_x, 
                        position_y = $position_y,
                        type = $type,
                        cost = $cost
                    WHERE shop_id = $shop_id;
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Updated Shop: ID {ShopId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving Shop ID {ShopId}: {ex.Message}");
            }
        }

        public void Load(SqliteDataReader reader)
        {
            ShopId = reader.GetInt32(reader.GetOrdinal("shop_id"));
            int posX = reader.GetInt32(reader.GetOrdinal("position_x"));
            int posY = reader.GetInt32(reader.GetOrdinal("position_y"));

            int typeOrdinal = reader.GetOrdinal("type");
            Type = reader.IsDBNull(typeOrdinal) ? "Food" : reader.GetString(typeOrdinal);

            int costOrdinal = reader.GetOrdinal("cost");
            Cost = reader.IsDBNull(costOrdinal) ? 0 : reader.GetInt32(costOrdinal);

            _position = new Vector2(posX * GameWorld.TILE_SIZE, posY * GameWorld.TILE_SIZE);
            PositionX = posX;
            PositionY = posY;

            InitializeShopState();
            Debug.WriteLine($"Loaded Shop ID: {ShopId}, Type: {Type}, Cost: {Cost} at tile ({PositionX},{PositionY}), Default Size ({_widthInTiles}x{_heightInTiles})");
        }
    }
} 