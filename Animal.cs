using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        bool[,] walkableMap = new bool[1280, 720];
        AStarPathfinding pathfinder;

        public Animal()
        {
            for (int x = 0; x < 1280; x++)
                for (int y = 0; y < 720; y++)
                    walkableMap[x, y] = true;

            pathfinder = new AStarPathfinding(1280, 720, walkableMap);
        }

        public void PathfindTo(Vector2 pos)
        {
            path = pathfinder.FindPath((int)position.X, (int)position.Y, (int)pos.X, (int)pos.Y);
        }

        public void LoadContent(ContentManager contentManager)
        {
            sprite = contentManager.Load<Texture2D>("NibblingGoat");
        }

        public void Update(GameTime gameTime)
        {
            if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float remainingMove = speed * deltaTime;

            while (remainingMove > 0 && currentNodeIndex < path.Count)
            {
                Node targetNode = path[currentNodeIndex];
                Vector2 targetPosition = new Vector2(targetNode.X, targetNode.Y);
                Vector2 direction = targetPosition - position;
                float distance = direction.Length();

                if (distance <= remainingMove)
                {
                    // Move all the way to the node and subtract the distance
                    position = targetPosition;
                    currentNodeIndex++;
                    remainingMove -= distance;
                }
                else
                {
                    // Move as far as we can toward the node
                    direction.Normalize();
                    position += direction * remainingMove;
                    remainingMove = 0;
                }
            }

            // If we've reached the end of the path, reset
            if (currentNodeIndex >= path.Count)
            {
                path = null;
                currentNodeIndex = 0;
            }
        }




        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(sprite, position, new Rectangle(0,0, 16, 16), Color.White, 0f, new Vector2(0,0), 4f, SpriteEffects.None, 0);
        }
    }
}
