using Microsoft.Xna.Framework;
using System.Collections.Generic;
using ZooTycoonManager.Enums;

namespace ZooTycoonManager.Components
{
    public class HabitatComponent : Component
    {
        public List<GameObject> fences { get; set; } = new List<GameObject>();
        private HabitatSizeType _size;

        public HabitatComponent(HabitatSizeType size)
        {
            _size = size;
        }

        public override void Initialize()
        {
            Vector2 centerTile = GameWorld.PixelToTile(Owner.Transform.Position);

            int radius = GetEnclosureRadius();
            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;

            for (int x = startX; x <= endX; x++)
            {
                fences.Add(GameWorld.Instance.Instantiate(EntityFactory.CreateFence(GameWorld.TileToPixel(new Vector2(x, startY)))));
                fences.Add(GameWorld.Instance.Instantiate(EntityFactory.CreateFence(GameWorld.TileToPixel(new Vector2(x, endY)))));
                //PlaceFenceTile(new Vector2(x, startY));
                //PlaceFenceTile(new Vector2(x, endY));
            }

            for (int y = startY + 1; y < endY; y++)
            {
                fences.Add(GameWorld.Instance.Instantiate(EntityFactory.CreateFence(GameWorld.TileToPixel(new Vector2(startX, y)))));
                fences.Add(GameWorld.Instance.Instantiate(EntityFactory.CreateFence(GameWorld.TileToPixel(new Vector2(endX, y)))));
                //PlaceFenceTile(new Vector2(startX, y));
                //PlaceFenceTile(new Vector2(endX, y));
            }
        }

        public bool ContainsPosition(Vector2 position)
        {
            int radiusInTiles = GetEnclosureRadius();
            int diameterInPixels = ((radiusInTiles * 2) + 1) * GameWorld.TILE_SIZE;
            return new Rectangle(
                (int)(Owner.Transform.Position.X - diameterInPixels / 2),
                (int)(Owner.Transform.Position.Y - diameterInPixels / 2),
                diameterInPixels,
                diameterInPixels
            ).Contains(position);
        }

        private int GetEnclosureRadius()
        {
            switch (_size)
            {
                case HabitatSizeType.Small: return 2;
                case HabitatSizeType.Medium: return 4;
                case HabitatSizeType.Large: return 6;
                default: return 4;
            }
        }

        public Vector2[] GetWalkableTiles()
        {
            Vector2 centerTile = GameWorld.PixelToTile(Owner.Transform.Position);
            int radius = GetEnclosureRadius();
            List<Vector2> walkableTiles = new List<Vector2>();

            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;

            for (int x = startX + 1; x < endX; x++)
            {
                for (int y = startY + 1; y < endY; y++)
                {
                    walkableTiles.Add(new Vector2(x, y));
                }
            }

            return walkableTiles.ToArray();
        }

        public Vector2[] GetOuterTilesWithRoads()
        {
            Vector2 centerTile = GameWorld.PixelToTile(Owner.Transform.Position);
            int radius = GetEnclosureRadius();
            List<Vector2> outerTiles = new List<Vector2>();
            List<Vector2> roadTiles = new List<Vector2>();

            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;

            int outerStartX = startX - 1;
            int outerStartY = startY - 1;
            int outerEndX = endX + 1;
            int outerEndY = endY + 1;

            for (int x = outerStartX; x <= outerEndX; x++)
            {
                outerTiles.Add(new Vector2(x, outerStartY));
                outerTiles.Add(new Vector2(x, outerEndY));
            }

            for (int y = outerStartY + 1; y < outerEndY; y++)
            {
                outerTiles.Add(new Vector2(outerStartX, y));
                outerTiles.Add(new Vector2(outerEndX, y));
            }

            foreach (var tile in outerTiles)
            {
                if (GameWorld.Instance.RoadTiles.Contains(((int)tile.X, (int)tile.Y)))
                {
                    roadTiles.Add(tile);
                }
            }

            return roadTiles.ToArray();
        }
    }
}
