using Microsoft.Xna.Framework;
using System;

namespace ZooTycoonManager.Components
{
    public class AnimalComponent : Component
    {
        GameObject _habitat;
        float timeSinceLastRandomWalk = 0f;
        const float RANDOM_WALK_INTERVAL = 3f;

        public AnimalComponent(GameObject habitatObject)
        {
            _habitat = habitatObject;
        }

        private void WalkToRandomSpotInHabitat()
        {
            var habitatComponent = _habitat.GetComponent<HabitatComponent>();
            var walkableTiles = habitatComponent.GetWalkableTiles();
            var rand = new Random();

            var randomTile = walkableTiles[rand.Next(walkableTiles.Length)];
            bool[,] tempWalkableMap = new bool[100, 100];
            for (int x = 0; x < 100; x++)
                for (int y = 0; y < 100; y++)
                    tempWalkableMap[x, y] = true;
            Owner.GetComponent<MovableComponent>().PathfindTo(randomTile, tempWalkableMap);
        }

        public override void Update(GameTime gameTime)
        {
            timeSinceLastRandomWalk += GameWorld.Instance.deltaTime;
            if (timeSinceLastRandomWalk >= RANDOM_WALK_INTERVAL)
            {
                timeSinceLastRandomWalk = 0f;
                WalkToRandomSpotInHabitat();
            }
        }
    }
}
