using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ZooTycoonManager
{
    public class Visitor
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

        private Thread _updateThread;
        private Thread _pathfindingThread;
        private List<Node> _newlyCalculatedPath;
        private readonly object _pathLock = new object();
        private readonly object _positionLock = new object();
        private bool _isRunning = true;

        public bool IsPathfinding { get; private set; }
        private Vector2 _pathfindingStartPos;
        private Vector2 _pathfindingTargetPos;

        public Visitor(Vector2 spawnPosition)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            position = spawnPosition;

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
                            Debug.WriteLine($"Visitor {GetHashCode()}: Deciding to visit a habitat at position {visitPosition.Value}");
                            PathfindTo(visitPosition.Value);
                            return;
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
                            Debug.WriteLine($"Visitor {GetHashCode()}: Received new path with {path.Count} nodes");
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
                Debug.WriteLine($"Visitor {GetHashCode()}: Reached destination at position {position}");
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
    }
}
