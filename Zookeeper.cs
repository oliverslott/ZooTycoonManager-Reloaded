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
        Texture2D sprite;
        List<Node> path;
        int currentNodeIndex = 0;
        float speed = 100f;
        AStarPathfinding pathfinder;
        private Habitat currentHabitat;
        private Random random = new Random();
        private float timeSinceLastRandomWalk = 0f;
        private const float RANDOM_WALK_INTERVAL = 3f; // Time in seconds between random walks

        private Thread _pathfindingWorkerThread;
        private readonly AutoResetEvent _pathfindingRequestEvent = new AutoResetEvent(false);
        private volatile bool _workerThreadRunning = true;
        private Vector2 _requestedPathfindingStartPos;
        private Vector2 _requestedPathfindingTargetPos;
        private List<Node> _pendingPathResult;
        private readonly object _pendingPathResultLock = new object();

        public bool IsPathfinding { get; private set; }

        private Vector2 _position;
        private int _positionX;
        private int _positionY;

        public int ZookeeperId { get; set; }
        public string Name { get; set; }
        public int Upkeep { get; set; }
        public int? HabitatId { get; set; }

        public string Description => $"Zookeeper: {Name} at ({_position.X:F0}, {_position.Y:F0})";

        public Vector2 Position
        {
            get => _position;
            private set
            {
                _position = value;
                // Update database position properties with tile coordinates
                Vector2 tilePos = GameWorld.PixelToTile(value);
                _positionX = (int)tilePos.X;
                _positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => _positionX;
        public int PositionY => _positionY;

        public Zookeeper(int zookeeperId = 1)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            Position = new Vector2(GameWorld.TILE_SIZE * 5, GameWorld.TILE_SIZE * 5);
            ZookeeperId = zookeeperId;
            
            Name = "Man";



            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL; // Make Zookeeper act on first update

            _pathfindingWorkerThread = new Thread(PathfindingWorkerLoop);
            _pathfindingWorkerThread.Name = $"Zookeeper_{GetHashCode()}_PathWorker";
            _pathfindingWorkerThread.IsBackground = true;
            _pathfindingWorkerThread.Start();
        }

        public void SetHabitat(Habitat habitat)
        {
            currentHabitat = habitat;
            HabitatId = habitat.HabitatId;
        }

        private void TryRandomWalk(GameTime gameTime)
        {
            if (currentHabitat == null || IsPathfinding) return;

            timeSinceLastRandomWalk += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeSinceLastRandomWalk >= RANDOM_WALK_INTERVAL)
            {
                timeSinceLastRandomWalk = 0f;

                // Get a random position within the habitat
                Vector2 centerTile = GameWorld.PixelToTile(currentHabitat.GetCenterPosition());
                int halfWidth = (currentHabitat.GetWidth() - 1) / 2;  // Subtract 1 to account for inclusive bounds
                int halfHeight = (currentHabitat.GetHeight() - 1) / 2;

                // Generate random position within habitat bounds, including edges
                int randomX = random.Next((int)centerTile.X - halfWidth, (int)centerTile.X + halfWidth + 1);
                int randomY = random.Next((int)centerTile.Y - halfHeight, (int)centerTile.Y + halfHeight + 1);

                Vector2 randomTilePos = new Vector2(randomX, randomY);
                Vector2 randomPixelPos = GameWorld.TileToPixel(randomTilePos);

                // Only pathfind if the position is walkable and within grid bounds
                if (randomX >= 0 && randomX < GameWorld.GRID_WIDTH &&
                    randomY >= 0 && randomY < GameWorld.GRID_HEIGHT &&
                    GameWorld.Instance.WalkableMap[randomX, randomY])
                {
                    PathfindTo(randomPixelPos);
                }
            }
        }
        public void SetPosition(Vector2 position)
        {
            _position = position;
        }
        private void PathfindingWorkerLoop()
        {
            while (_workerThreadRunning)
            {
                _pathfindingRequestEvent.WaitOne();
                if (!_workerThreadRunning) break;

                List<Node> calculatedPath = null;
                Stopwatch stopwatch = new Stopwatch();
                try
                {
                    stopwatch.Start();
                    Vector2 startTile = GameWorld.PixelToTile(_requestedPathfindingStartPos);
                    Vector2 targetTile = GameWorld.PixelToTile(_requestedPathfindingTargetPos);

                    // PathfindTo method ensures 'pathfinder' member is up-to-date with WalkableMap
                    calculatedPath = pathfinder.FindPath(
                        (int)startTile.X, (int)startTile.Y,
                        (int)targetTile.X, (int)targetTile.Y);

                    stopwatch.Stop();
                    // Debug.WriteLine($"Animal Pathfinding (Worker {Thread.CurrentThread.Name}) took {stopwatch.ElapsedMilliseconds} ms.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in Zookeeper pathfinding worker thread ({Thread.CurrentThread.Name}): {ex.Message}");
                }
                finally
                {
                    lock (_pendingPathResultLock)
                    {
                        _pendingPathResult = calculatedPath;
                    }
                }
            }
            Debug.WriteLine($"Zookeeper Pathfinding Worker ({Thread.CurrentThread.Name}) exiting.");
        }

        public void PathfindTo(Vector2 targetDestination)
        {
            if (IsPathfinding)
            {
                Debug.WriteLine("Zookeeper is already pathfinding or a path request is pending. New request ignored.");
                return;
            }

            // Refresh pathfinder with updated walkable map. Worker thread will use this instance.
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            IsPathfinding = true; // Mark that a pathfinding process has started
            _requestedPathfindingStartPos = Position;  // Use Position property
            _requestedPathfindingTargetPos = targetDestination;

            lock (_pendingPathResultLock)
            {
                _pendingPathResult = null;
            }

            _pathfindingRequestEvent.Set(); // Signal the worker thread
        }
        public void LoadContent(ContentManager contentManager)
        {
            sprite = contentManager.Load<Texture2D>("zookeeper");
        }

        public void Update(GameTime gameTime)
        {
            TryRandomWalk(gameTime);

            if (IsPathfinding) // If a pathfinding task was initiated
            {
                bool pathProcessed = false;
                lock (_pendingPathResultLock)
                {
                    if (_pendingPathResult != null) // Check if the worker thread has produced a result
                    {
                        path = _pendingPathResult;
                        currentNodeIndex = 0;
                        _pendingPathResult = null; // Clear the result
                        pathProcessed = true;
                    }
                }

                if (pathProcessed)
                {
                    IsPathfinding = false; // Pathfinding process (request -> calculation -> processing) is complete
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
                Vector2 directionToNode = targetNodePosition - Position;
                float distanceToNode = directionToNode.Length();

                if (distanceToNode <= remainingMoveThisFrame)
                {
                    Position = targetNodePosition;
                    currentNodeIndex++;
                    remainingMoveThisFrame -= distanceToNode;
                }
                else
                {
                    if (distanceToNode > 0)
                    {
                        directionToNode.Normalize();
                        Vector2 newPosition = Position + directionToNode * remainingMoveThisFrame;
                        Position = newPosition;
                    }
                    remainingMoveThisFrame = 0;
                }
            }

            if (currentNodeIndex >= path.Count)
            {
                path = null;
                currentNodeIndex = 0;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (sprite == null) return;
            spriteBatch.Draw(sprite, Position, new Rectangle(0, 0, 32, 32), Color.White, 0f, new Vector2(8, 8), 2f, SpriteEffects.None, 0f);
        }



        public void Save(SqliteTransaction transaction)
        {
            using (var command = new SqliteCommand("INSERT INTO Zookeeper (ZookeeperId, Name, Upkeep, HabitatId, PositionX, PositionY) VALUES (@ZookeeperId, @Name, @Upkeep, @HabitatId, @PositionX, @PositionY)", transaction.Connection))
            {
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@ZookeeperId", ZookeeperId);
                command.Parameters.AddWithValue("@Name", Name);
                command.Parameters.AddWithValue("@Upkeep", Upkeep);
                command.Parameters.AddWithValue("@HabitatId", HabitatId ?? -1); // Use -1 for NULL
                command.Parameters.AddWithValue("@PositionX", _position.X);
                command.Parameters.AddWithValue("@PositionY", _position.Y);



                command.ExecuteNonQuery();
            }
        }

        public void Load(SqliteDataReader reader)
        {
            ZookeeperId = reader.GetInt32(reader.GetOrdinal("ZookeeperId"));
            Name = reader.GetString(reader.GetOrdinal("Name"));
            Upkeep = reader.GetInt32(reader.GetOrdinal("Upkeep"));

            // Handle NULL values for HabitatId
            if (!reader.IsDBNull(reader.GetOrdinal("HabitatId")))
            {
                HabitatId = reader.GetInt32(reader.GetOrdinal("HabitatId"));
            }
            else
            {
                HabitatId = null;
            }

            float positionX = reader.GetFloat(reader.GetOrdinal("PositionX"));
            float positionY = reader.GetFloat(reader.GetOrdinal("PositionY"));
            _position = new Vector2(positionX, positionY);
        }
    }
}
