using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class Zookeeper : ISaveable, ILoadable
    {
        private Texture2D sprite;
        private Vector2 position;
        private List<Node> path;
        private int currentNodeIndex = 0;
        private float speed = 45f;
        private AStarPathfinding pathfinder;
        private Random random = new Random();
        private float timeSinceLastAction = 0f;
        private const float ACTION_INTERVAL = 5f;
        private const float FEEDING_DURATION = 3f;
        private float currentFeedingTime = 0f;
        private bool _isFeeding = false;

        private Habitat _assignedHabitat;
        private const int ANIMAL_HUNGER_THRESHOLD = 30;

        private readonly object _positionLock = new object();
        private bool _isRunning = true;
        private bool _isEnRouteToFeedOrPerformTask = false;

        // Database
        public int ZookeeperId { get; set; }
        public string Name { get; set; }
        public int Upkeep { get; set; }
        private int _positionX;
        private int _positionY;
        public int AssignedHabitatId { get; private set; }

        public Vector2 Position
        {
            get => position;
            private set
            {
                position = value;
                Vector2 tilePos = GameWorld.PixelToTile(value);
                _positionX = (int)tilePos.X;
                _positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => _positionX;
        public int PositionY => _positionY;

        public Zookeeper(Vector2 spawnTilePosition, int zookeeperId, Habitat assignedHabitat, string name, int upkeep)
        {
            Position = GameWorld.TileToPixel(spawnTilePosition);
            ZookeeperId = zookeeperId;
            _assignedHabitat = assignedHabitat;
            AssignedHabitatId = assignedHabitat?.HabitatId ?? -1;
            Name = name;
            Upkeep = upkeep;

            InitializeBehavioralState();
            StartUpdateThread();
        }

        public Zookeeper()
        {
        }

        private void ZookeeperUpdateLoop()
        {
            GameTime gameTime = new GameTime();
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan elapsed = currentTime - lastUpdate;
                gameTime.ElapsedGameTime = elapsed;
                lastUpdate = currentTime;

                Update(gameTime);
                Thread.Sleep(16); // 60 FPS
            }
        }

        public void InitializeBehavioralState()
        {
            _isRunning = true;
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            path = null;
            currentNodeIndex = 0;
            timeSinceLastAction = ACTION_INTERVAL;
            currentFeedingTime = 0f;
            _isFeeding = false;
            _isEnRouteToFeedOrPerformTask = false;
        }

        public void StartUpdateThread()
        {
            if (ZookeeperId == 0 && string.IsNullOrEmpty(Name)) 
            {
                Debug.WriteLine($"Warning: Attempted to start update thread for Zookeeper with ID {ZookeeperId} and Name '{Name}' before proper initialization.");
                return;
            }
            Thread updateThread = new Thread(ZookeeperUpdateLoop);
            string threadNameSuffix = Name?.Replace(" ", "") ?? "Unknown";
            updateThread.Name = $"Zookeeper_{ZookeeperId}_{threadNameSuffix}_{GetHashCode()}_Update";
            updateThread.IsBackground = true;
            updateThread.Start();
        }

        private void PerformNextActionDecision()
        {
            if (path != null && path.Count > 0 && currentNodeIndex < path.Count) return;
            if (_isFeeding) return;

            if (_assignedHabitat != null)
            {
                bool animalsNeedFeeding = false;
                foreach (var animal in _assignedHabitat.GetAnimals())
                {
                    if (animal.Hunger > ANIMAL_HUNGER_THRESHOLD)
                    {
                        animalsNeedFeeding = true;
                        break;
                    }
                }

                if (animalsNeedFeeding)
                {
                    List<Vector2> visitingSpots = _assignedHabitat.GetWalkableVisitingSpots();
                    if (visitingSpots.Count > 0)
                    {
                        Vector2 targetSpot = visitingSpots[random.Next(visitingSpots.Count)]; 
                        PathfindTo(targetSpot);
                        _isEnRouteToFeedOrPerformTask = true;
                        Debug.WriteLine($"Zookeeper {ZookeeperId}: Animals in habitat {_assignedHabitat.Name} need feeding. Pathfinding to spot {targetSpot} adjacent to habitat.");
                    }
                    else
                    {
                        _isEnRouteToFeedOrPerformTask = false;
                        Debug.WriteLine($"Zookeeper {ZookeeperId}: No accessible visiting spots found for habitat {_assignedHabitat.Name}. Performing random walk instead.");
                        PerformRandomWalk();
                    }
                    return;
                }
            }
            
            _isEnRouteToFeedOrPerformTask = false;
            PerformRandomWalk();
        }

        private void PerformRandomWalk()
        {
            List<Vector2> walkableTiles = GameWorld.Instance.GetWalkableTileCoordinates();

            if (walkableTiles.Count > 0)
            {
                Vector2 randomTilePos = walkableTiles[random.Next(walkableTiles.Count)];
                PathfindTo(randomTilePos);
                Debug.WriteLine($"Zookeeper {ZookeeperId}: Performing random walk to {randomTilePos}.");
            }
            else
            {
                Debug.WriteLine($"Zookeeper {ZookeeperId}: No walkable tiles found for random walk.");
            }
        }

        public void PathfindTo(Vector2 targetTileDestination)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            Vector2 startTile = GameWorld.PixelToTile(position);

            List<Node> calculatedPath = pathfinder.FindPath(
                (int)startTile.X, (int)startTile.Y,
                (int)targetTileDestination.X, (int)targetTileDestination.Y);

            path = calculatedPath;
            currentNodeIndex = 0;

            if (path != null && path.Count > 0)
            {

            }
            else
            {
                path = null;
            }
        }

        public void LoadContent(ContentManager contentManager)
        {
            sprite = contentManager.Load<Texture2D>("zookeeper");
        }

        private void Update(GameTime gameTime)
        {
            UpdateActivity(gameTime);

            if (_assignedHabitat != null && !_isFeeding && !_isEnRouteToFeedOrPerformTask)
            {
                bool animalsNeedFeeding = false;
                foreach (var animal in _assignedHabitat.GetAnimals())
                {
                    if (animal.Hunger > ANIMAL_HUNGER_THRESHOLD)
                    {
                        animalsNeedFeeding = true;
                        break;
                    }
                }

                if (animalsNeedFeeding)
                {
                    if (path != null) 
                    {
                        Debug.WriteLine($"Zookeeper {ZookeeperId}: Interrupting random walk to feed animals in habitat {_assignedHabitat.Name}.");
                        path = null; 
                        currentNodeIndex = 0;
                    }
                    timeSinceLastAction = 0f;
                    PerformNextActionDecision();
                }
            }


            if (!_isFeeding && (path == null || path.Count == 0 || currentNodeIndex >= path.Count))
            {
                timeSinceLastAction += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (timeSinceLastAction >= ACTION_INTERVAL)
                {
                    timeSinceLastAction = 0f;
                    PerformNextActionDecision();
                }
            }

            UpdatePathFollowing(gameTime);
            HandlePathCompletion();
        }

        private void UpdateActivity(GameTime gameTime)
        {
            if (_isFeeding)
            {
                currentFeedingTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (currentFeedingTime >= FEEDING_DURATION)
                {
                    Debug.WriteLine($"Zookeeper {ZookeeperId}: Finished feeding animals in habitat {_assignedHabitat.Name}.");
                    if (_assignedHabitat != null)
                    {
                        foreach (var animal in _assignedHabitat.GetAnimals())
                        {
                            animal.Hunger = 0;
                        }
                    }
                    _isFeeding = false;
                    currentFeedingTime = 0f;
                    PerformNextActionDecision();
                }
            }
        }

        private void UpdatePathFollowing(GameTime gameTime)
        {
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
                        Position = targetNodePosition;
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
                            Position = position + directionToNode * remainingMoveThisFrame;
                        }
                    }
                    remainingMoveThisFrame = 0;
                }
            }
        }

        private void HandlePathCompletion()
        {
            if (path != null && currentNodeIndex >= path.Count)
            {
                path = null;
                currentNodeIndex = 0;

                if (_isEnRouteToFeedOrPerformTask && _assignedHabitat != null)
                {
                    bool animalsStillNeedFeeding = false;
                    foreach (var animal in _assignedHabitat.GetAnimals())
                    {
                        if (animal.Hunger > ANIMAL_HUNGER_THRESHOLD)
                        {
                            animalsStillNeedFeeding = true;
                            break;
                        }
                    }

                    if (animalsStillNeedFeeding)
                    {
                        _isFeeding = true;
                        currentFeedingTime = 0f;
                        Debug.WriteLine($"Zookeeper {ZookeeperId}: Arrived at habitat {_assignedHabitat.Name} vicinity to feed animals.");
                    }
                    else
                    {
                        Debug.WriteLine($"Zookeeper {ZookeeperId}: Arrived at habitat {_assignedHabitat.Name} vicinity, but animals no longer need feeding.");
                        timeSinceLastAction = ACTION_INTERVAL;
                    }
                }
                else
                {
                    timeSinceLastAction = ACTION_INTERVAL;
                }
                _isEnRouteToFeedOrPerformTask = false;
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

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$zookeeper_id", ZookeeperId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$upkeep", Upkeep);
            command.Parameters.AddWithValue("$habitat_id", AssignedHabitatId == -1 ? (object)DBNull.Value : AssignedHabitatId);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Zookeeper 
                SET name = $name, 
                    upkeep = $upkeep,
                    habitat_id = $habitat_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE zookeeper_id = $zookeeper_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Zookeeper (zookeeper_id, name, upkeep, habitat_id, position_x, position_y)
                    VALUES ($zookeeper_id, $name, $upkeep, $habitat_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Zookeeper: ID {ZookeeperId}, Name {Name}");
            }
            else
            {
                Debug.WriteLine($"Updated Zookeeper: ID {ZookeeperId}, Name {Name}");
            }
        }

        public void Load(SqliteDataReader reader)
        {
            ZookeeperId = reader.GetInt32(reader.GetOrdinal("zookeeper_id"));
            Name = reader.GetString(reader.GetOrdinal("name"));
            Upkeep = reader.GetInt32(reader.GetOrdinal("upkeep"));
            AssignedHabitatId = reader.IsDBNull(reader.GetOrdinal("habitat_id")) ? -1 : reader.GetInt32(reader.GetOrdinal("habitat_id"));
            int posX = reader.GetInt32(reader.GetOrdinal("position_x"));
            int posY = reader.GetInt32(reader.GetOrdinal("position_y"));

            Position = GameWorld.TileToPixel(new Vector2(posX, posY));
        }


        public void SetAssignedHabitat(Habitat habitat)
        {
            _assignedHabitat = habitat;
            AssignedHabitatId = habitat?.HabitatId ?? -1;
        }
    }
}