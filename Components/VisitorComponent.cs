using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZooTycoonManager.Components
{
    public class VisitorComponent : Component
    {
        private List<GameObject> _habitatsToVisit = new List<GameObject>();
        private List<GameObject> _visitedHabitats = new List<GameObject>();

        public override void Initialize()
        {
            foreach (var habitat in GameWorld.Instance.GetHabitats())
            {
                _habitatsToVisit.Add(habitat);
            }

            if(_habitatsToVisit.Count > 0 && !AttemptToVisitHabitat(_habitatsToVisit[0]))
            {
                //Couldn't get to it, just adding it to visited list
            }
        }

        public bool AttemptToVisitHabitat(GameObject habitat)
        {
            Random rand = new Random();

            var tiles = habitat.GetComponent<HabitatComponent>().GetOuterTilesWithRoads();
            if(tiles.Length == 0)
            {
                Debug.WriteLine("Path to habitat not found");
                return false;
            }
            var randomPos = tiles[rand.Next(tiles.Length)];

            bool[,] tempWalkableMap = new bool[100, 100];
            for (int x = 0; x < 100; x++)
                for (int y = 0; y < 100; y++)
                    tempWalkableMap[x, y] = true;
            Owner.GetComponent<MovableComponent>().PathfindTo(randomPos, tempWalkableMap);

            return true;
        }
    }
}
