using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class Animal
    {
        Texture2D sprite;
        Vector2 position;
        List<Node> path;
        int currentNodeIndex = 0;
        float speed = 300f;
        AStarPathfinding pathfinder;
        private Habitat currentHabitat;
        private Random random = new Random();
        private float timeSinceLastRandomWalk = 0f;
        private const float RANDOM_WALK_INTERVAL = 3f; // Time in seconds between random walks

        private Thread _pathfindingThread;
        private List<Node> _newlyCalculatedPath;
        private readonly object _pathLock = new object();

        public bool IsPathfinding { get; private set; }

        private Vector2 _pathfindingStartPos;
        private Vector2 _pathfindingTargetPos;

        public Animal()
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            position = new Vector2(GameWorld.TILE_SIZE * 5, GameWorld.TILE_SIZE * 5);
        }

        public void SetHabitat(Habitat habitat)
        {
            currentHabitat = habitat;
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

        public void SetPosition(Vector2 newPosition)
        {
            position = newPosition;
        }

        private void PerformPathfinding()
        {
            List<Node> calculatedPath = null;
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                Vector2 startTile = GameWorld.PixelToTile(_pathfindingStartPos);
                Vector2 targetTile = GameWorld.PixelToTile(_pathfindingTargetPos);
                
                calculatedPath = pathfinder.FindPath(
                    (int)startTile.X, (int)startTile.Y,
                    (int)targetTile.X, (int)targetTile.Y);

                stopwatch.Stop();
                Debug.WriteLine($"Pathfinding took {stopwatch.ElapsedMilliseconds} ms.");
            }
            catch (ThreadAbortException tae)
            {
                Debug.WriteLine($"Animal pathfinding thread ({Thread.CurrentThread.Name}) aborted: {tae.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in animal pathfinding thread ({Thread.CurrentThread.Name}): {ex.Message}");
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
            if (IsPathfinding)
            {
                Debug.WriteLine("Animal is already pathfinding. New request ignored.");
                return;
            }

            // Refresh pathfinder with updated walkable map
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            IsPathfinding = true;
            _pathfindingStartPos = this.position;
            _pathfindingTargetPos = targetDestination;

            lock (_pathLock)
            {
                _newlyCalculatedPath = null;
            }

            _pathfindingThread = new Thread(new ThreadStart(PerformPathfinding));
            _pathfindingThread.Name = $"Animal_{GetHashCode()}_Pathfinder";
            _pathfindingThread.IsBackground = true;
            _pathfindingThread.Start();
        }

        public void LoadContent(ContentManager contentManager)
        {
            sprite = contentManager.Load<Texture2D>("NibblingGoat");
        }

        public void Update(GameTime gameTime)
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
                    position = targetNodePosition;
                    currentNodeIndex++;
                    remainingMoveThisFrame -= distanceToNode;
                }
                else
                {
                    if (distanceToNode > 0)
                    {
                        directionToNode.Normalize();
                        position += directionToNode * remainingMoveThisFrame;
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
            spriteBatch.Draw(sprite, position, new Rectangle(0, 0, 16, 16), Color.White, 0f, new Vector2(8, 8), 2f, SpriteEffects.None, 0f);
        }

        public void StopPathfindingThread()
        {
            if (_pathfindingThread != null && _pathfindingThread.IsAlive)
            {
                IsPathfinding = false;
            }
            _pathfindingThread = null;
        }
    }
}
