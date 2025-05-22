using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ZooTycoonManager
{
    public class Visitor : ISaveable, ILoadable
    {
        private Texture2D sprite;
        private Vector2 position;
        private List<Node> path;
        private int currentNodeIndex = 0;
        private float speed = 80f; // Slightly slower than animals
        private AStarPathfinding pathfinder;
        private Random random = new Random();
        private float timeSinceLastRandomWalk = 0f;
        private const float RANDOM_WALK_INTERVAL = 5f; // Longer interval than animals
        private const float VISIT_DURATION = 4f;  // How long to stay at a habitat
        private float currentVisitTime = 0f;
        private Habitat currentHabitat = null;

        private Thread _updateThread;
        private Thread _pathfindingThread;
        private List<Node> _newlyCalculatedPath;
        private readonly object _pathLock = new object();
        private readonly object _positionLock = new object();
        private bool _isRunning = true;

        public bool IsPathfinding { get; private set; }
        private Vector2 _pathfindingStartPos;
        private Vector2 _pathfindingTargetPos;

        // Database properties
        public int VisitorId { get; set; }
        public string Name { get; set; }
        public int Money { get; set; }
        public int Mood { get; set; }
        public int Hunger { get; set; }
        public int? HabitatId { get; set; }
        public int? ShopId { get; set; }
        private int _positionX;
        private int _positionY;

        public Vector2 Position 
        { 
            get => position;
            private set
            {
                position = value;
                // Update database position properties with tile coordinates
                Vector2 tilePos = GameWorld.PixelToTile(value);
                _positionX = (int)tilePos.X;
                _positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => _positionX;
        public int PositionY => _positionY;

        public Visitor(Vector2 spawnPosition, int visitorId = 0)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            Position = spawnPosition;
            VisitorId = visitorId;

            // Initialize default values
            Name = "Visitor";
            Money = 100;
            Mood = 100;
            Hunger = 0;
            HabitatId = null;
            ShopId = null;

            // Start the update thread
            _updateThread = new Thread(UpdateLoop);
            _updateThread.Name = $"Visitor_{GetHashCode()}_Update";
            _updateThread.IsBackground = true;
            _updateThread.Start();
        }

        private void UpdateLoop()
        {
            GameTime gameTime = new GameTime();
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan elapsed = currentTime - lastUpdate;
                gameTime.ElapsedGameTime = elapsed;
                gameTime.TotalGameTime += elapsed;
                lastUpdate = currentTime;

                Update(gameTime);
                Thread.Sleep(16); // Approximately 60 FPS
            }
        }

        private void TryRandomWalk(GameTime gameTime)
        {
            if (IsPathfinding) return;

            timeSinceLastRandomWalk += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeSinceLastRandomWalk >= RANDOM_WALK_INTERVAL)
            {
                timeSinceLastRandomWalk = 0f;

                // If we're currently visiting a habitat, just return
                if (currentHabitat != null)
                {
                    return;
                }

                // Get all habitats from GameWorld
                var habitats = GameWorld.Instance.GetHabitats();
                if (habitats.Count > 0)
                {
                    // 70% chance to visit a habitat, 30% chance to random walk
                    if (random.NextDouble() < 0.7)
                    {
                        // Pick a random habitat
                        var randomHabitat = habitats[random.Next(habitats.Count)];
                        var visitPosition = randomHabitat.GetRandomFencePosition();
                        
                        if (visitPosition.HasValue)
                        {
                            // Try to enter the habitat
                            if (randomHabitat.TryEnterHabitatSync(this))
                            {
                                Debug.WriteLine($"Visitor {GetHashCode()}: Entering habitat at position {visitPosition.Value}");
                                currentHabitat = randomHabitat;
                                currentVisitTime = 0f;
                                PathfindTo(visitPosition.Value);
                                return;
                            }
                            else
                            {
                                Debug.WriteLine($"Visitor {GetHashCode()}: Habitat is full, taking random walk instead");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Visitor {GetHashCode()}: Failed to find a valid position next to habitat");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Visitor {GetHashCode()}: Deciding to take a random walk instead of visiting habitat");
                    }
                }
                else
                {
                    Debug.WriteLine($"Visitor {GetHashCode()}: No habitats available, taking random walk");
                }

                // Fallback to random walk if no habitats or random choice
                int randomX = random.Next(0, GameWorld.GRID_WIDTH);
                int randomY = random.Next(0, GameWorld.GRID_HEIGHT);

                Vector2 randomTilePos = new Vector2(randomX, randomY);
                Vector2 randomPixelPos = GameWorld.TileToPixel(randomTilePos);

                if (GameWorld.Instance.WalkableMap[randomX, randomY])
                {
                    Debug.WriteLine($"Visitor {GetHashCode()}: Random walking to position {randomPixelPos}");
                    PathfindTo(randomPixelPos);
                }
            }
        }

        private void PerformPathfinding()
        {
            List<Node> calculatedPath = null;
            try
            {
                Vector2 startTile = GameWorld.PixelToTile(_pathfindingStartPos);
                Vector2 targetTile = GameWorld.PixelToTile(_pathfindingTargetPos);
                
                calculatedPath = pathfinder.FindPath(
                    (int)startTile.X, (int)startTile.Y,
                    (int)targetTile.X, (int)targetTile.Y);
            }
            catch (ThreadAbortException tae)
            {
                Debug.WriteLine($"Visitor pathfinding thread ({Thread.CurrentThread.Name}) aborted: {tae.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in visitor pathfinding thread ({Thread.CurrentThread.Name}): {ex.Message}");
            }
            finally
            {
                lock (_pathLock)
                {
                    _newlyCalculatedPath = calculatedPath;
                }
            }
        }

        public void PathfindTo(Vector2 targetDestination)
        {
            if (IsPathfinding) return;

            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            IsPathfinding = true;
            _pathfindingStartPos = position;
            _pathfindingTargetPos = targetDestination;

            lock (_pathLock)
            {
                _newlyCalculatedPath = null;
            }

            _pathfindingThread = new Thread(new ThreadStart(PerformPathfinding));
            _pathfindingThread.Name = $"Visitor_{GetHashCode()}_Pathfinder";
            _pathfindingThread.IsBackground = true;
            _pathfindingThread.Start();
        }

        public void LoadContent(ContentManager contentManager)
        {
            sprite = contentManager.Load<Texture2D>("Pawn_Blue_Cropped_resized");
        }

        private void Update(GameTime gameTime)
        {
            // Check visit duration every frame
            if (currentHabitat != null)
            {
                // Only increment visit time if we're not currently pathfinding and have no active path
                if (!IsPathfinding && (path == null || path.Count == 0))
                {
                    currentVisitTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    //Debug.WriteLine($"Visitor {GetHashCode()}: Current visit time: {currentVisitTime:F2}s / {VISIT_DURATION}s");
                    if (currentVisitTime >= VISIT_DURATION)
                    {
                        currentHabitat.LeaveHabitat(this);
                        currentHabitat = null;
                        currentVisitTime = 0f;
                        Debug.WriteLine($"Visitor {GetHashCode()}: Finished visiting habitat");
                    }
                }
            }

            TryRandomWalk(gameTime);

            if (IsPathfinding)
            {
                if (_pathfindingThread != null && !_pathfindingThread.IsAlive)
                {
                    lock (_pathLock)
                    {
                        if (_newlyCalculatedPath != null)
                        {
                            path = _newlyCalculatedPath;
                            currentNodeIndex = 0;
                            //Debug.WriteLine($"Visitor {GetHashCode()}: Received new path with {path.Count} nodes");
                        }
                        _newlyCalculatedPath = null;
                    }

                    IsPathfinding = false;
                    _pathfindingThread = null;
                }
            }

            if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
            {
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float remainingMoveThisFrame = speed * deltaTime;

            while (remainingMoveThisFrame > 0 && currentNodeIndex < path.Count)
            {
                Node targetNode = path[currentNodeIndex];
                Vector2 targetNodePosition = GameWorld.TileToPixel(new Vector2(targetNode.X, targetNode.Y));
                Vector2 directionToNode = targetNodePosition - position;
                float distanceToNode = directionToNode.Length();

                if (distanceToNode <= remainingMoveThisFrame)
                {
                    lock (_positionLock)
                    {
                        position = targetNodePosition;
                    }
                    currentNodeIndex++;
                    remainingMoveThisFrame -= distanceToNode;
                }
                else
                {
                    if (distanceToNode > 0)
                    {
                        directionToNode.Normalize();
                        lock (_positionLock)
                        {
                            position += directionToNode * remainingMoveThisFrame;
                        }
                    }
                    remainingMoveThisFrame = 0;
                }
            }

            if (currentNodeIndex >= path.Count)
            {
                //Debug.WriteLine($"Visitor {GetHashCode()}: Reached destination at position {position}");
                path = null;
                currentNodeIndex = 0;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (sprite == null) return;
            lock (_positionLock)
            {
                spriteBatch.Draw(sprite, position, new Rectangle(0, 0, 32, 32), Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);
            }
        }

        public Vector2 GetPosition()
        {
            lock (_positionLock)
            {
                return position;
            }
        }

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$visitor_id", VisitorId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$money", Money);
            command.Parameters.AddWithValue("$mood", Mood);
            command.Parameters.AddWithValue("$hunger", Hunger);
            command.Parameters.AddWithValue("$habitat_id", (object)HabitatId ?? DBNull.Value);
            command.Parameters.AddWithValue("$shop_id", (object)ShopId ?? DBNull.Value);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Visitor 
                SET name = $name, 
                    money = $money, 
                    mood = $mood, 
                    hunger = $hunger, 
                    habitat_id = $habitat_id, 
                    shop_id = $shop_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE visitor_id = $visitor_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Visitor (visitor_id, name, money, mood, hunger, habitat_id, shop_id, position_x, position_y)
                    VALUES ($visitor_id, $name, $money, $mood, $hunger, $habitat_id, $shop_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Visitor: ID {VisitorId}, Name: {Name}");
            }
            else
            {
                Debug.WriteLine($"Updated Visitor: ID {VisitorId}, Name: {Name}");
            }
        }

        public void Load(SqliteDataReader reader)
        {
            VisitorId = reader.GetInt32(0);
            Name = reader.GetString(1);
            Money = reader.GetInt32(2);
            Mood = reader.GetInt32(3);
            Hunger = reader.GetInt32(4);
            HabitatId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5);
            ShopId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6);
            int posX = reader.GetInt32(7);
            int posY = reader.GetInt32(8);

            // Convert tile position to pixel position
            Vector2 pixelPos = GameWorld.TileToPixel(new Vector2(posX, posY));
            Position = pixelPos;

            // Initialize other properties
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            path = null;
            currentNodeIndex = 0;
            timeSinceLastRandomWalk = 0f;
            currentVisitTime = 0f;
        }
    }
}
