namespace ZooTycoonManager
{
    public class Node
    {
        public int X, Y;
        public bool Walkable;
        public float GCost, HCost;
        public Node Parent;

        public float FCost => GCost + HCost;

        public Node(int x, int y, bool walkable)
        {
            X = x;
            Y = y;
            Walkable = walkable;
        }
    }
}
