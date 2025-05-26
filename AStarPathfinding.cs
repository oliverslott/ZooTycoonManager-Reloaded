using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class AStarPathfinding
    {
        private int width, height;
        private Node[,] grid;

        public AStarPathfinding(int width, int height, bool[,] walkableMap)
        {
            this.width = width;
            this.height = height;
            grid = new Node[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    grid[x, y] = new Node(x, y, walkableMap[x, y]);
        }

        public List<Node> FindPath(int startX, int startY, int endX, int endY)
        {
            Node startNode = grid[startX, startY];
            Node endNode = grid[endX, endY];

            var openSet = new List<Node> { startNode };
            var closedSet = new HashSet<Node>();

            while (openSet.Count > 0)
            {
                Node current = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();

                if (current == endNode)
                    return RetracePath(startNode, endNode);

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Node neighbor in GetNeighbors(current))
                {
                    if (!neighbor.Walkable || closedSet.Contains(neighbor))
                        continue;

                    float tentativeGCost = current.GCost + GetDistance(current, neighbor);
                    if (tentativeGCost < neighbor.GCost || !openSet.Contains(neighbor))
                    {
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = GetDistance(neighbor, endNode);
                        neighbor.Parent = current;

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return null; // No path found
        }

        private List<Node> RetracePath(Node startNode, Node endNode)
        {
            var path = new List<Node>();
            Node current = endNode;

            while (current != startNode)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        private List<Node> GetNeighbors(Node node)
        {
            var neighbors = new List<Node>();

            // Cardinal directions (N, S, E, W)
            // dx and dy arrays define the change in X and Y for each direction
            int[] dx = { 0, 0, 1, -1 }; // For E, W
            int[] dy = { 1, -1, 0, 0 }; // For S, N (assuming Y increases downwards)

            for (int i = 0; i < 4; i++) // Iterate over the 4 cardinal directions
            {
                int checkX = node.X + dx[i];
                int checkY = node.Y + dy[i];

                // Check if the neighbor is within grid bounds
                if (checkX >= 0 && checkX < width && checkY >= 0 && checkY < height)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
            return neighbors;
        }

        private float GetDistance(Node a, Node b)
        {
            int dstX = Math.Abs(a.X - b.X);
            int dstY = Math.Abs(a.Y - b.Y);
            return dstX + dstY; // Manhattan distance
        }
    }
}
