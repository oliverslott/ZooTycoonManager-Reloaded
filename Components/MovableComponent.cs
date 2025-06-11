using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ZooTycoonManager.Components
{
    public class MovableComponent : Component
    {
        public float Speed { get; set; } = 100;

        AStarPathfinding pathfinder;

        Vector2 _pathfindingStartPos;

        List<Node> path;

        int currentNodeIndex;

        public void PathfindTo(Vector2 targetTileDestination, bool[,] walkableTiles)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, walkableTiles);

            _pathfindingStartPos = Owner.Transform.Position;

            Vector2 startTile = GameWorld.PixelToTile(_pathfindingStartPos);

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

        private void UpdatePathFollowing(GameTime gameTime)
        {
            if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
            {
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float remainingMoveThisFrame = Speed * deltaTime;

            while (remainingMoveThisFrame > 0 && currentNodeIndex < path.Count)
            {
                Node targetNode = path[currentNodeIndex];
                Vector2 targetNodePosition = GameWorld.TileToPixel(new Vector2(targetNode.X, targetNode.Y));
                Vector2 directionToNode = targetNodePosition - Owner.Transform.Position;
                float distanceToNode = directionToNode.Length();

                if (distanceToNode <= remainingMoveThisFrame)
                {
                    Owner.Transform.Position = targetNodePosition;
                    currentNodeIndex++;
                    remainingMoveThisFrame -= distanceToNode;
                }
                else
                {
                    if (distanceToNode > 0)
                    {
                        directionToNode.Normalize();
                        Owner.Transform.Position = Owner.Transform.Position + directionToNode * remainingMoveThisFrame;
                    }
                    remainingMoveThisFrame = 0;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            UpdatePathFollowing(gameTime);
        }
    }
}
