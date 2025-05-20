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
    internal class Animal
    {
        Texture2D sprite;
        Vector2 position;
        List<Node> path;
        int currentNodeIndex = 0;
        float speed = 300f;
        AStarPathfinding pathfinder;

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
