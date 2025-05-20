using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;

namespace ZooTycoonManager
{
    public class Habitat
    {
        // Enclosure size constant
        public const int DEFAULT_ENCLOSURE_SIZE = 9;  // Total size of the enclosure (9x9)

        private Vector2 centerPosition;
        private int width;
        private int height;
        private List<Vector2> fencePositions;
        private List<Animal> animals;
        private static Texture2D fenceTexture;

        public static int GetEnclosureRadius()
        {
            return (DEFAULT_ENCLOSURE_SIZE - 1) / 2;
        }

        public Habitat(Vector2 centerPosition, int width, int height)
        {
            this.centerPosition = centerPosition;
            this.width = width;
            this.height = height;
            this.fencePositions = new List<Vector2>();
            this.animals = new List<Animal>();
        }

        public static void LoadContent(ContentManager content)
        {
            fenceTexture = content.Load<Texture2D>("fence");
        }

        public void AddFencePosition(Vector2 position)
        {
            fencePositions.Add(position);
        }

        public void AddAnimal(Animal animal)
        {
            animals.Add(animal);
        }

        public bool ContainsPosition(Vector2 position)
        {
            Vector2 tilePos = GameWorld.PixelToTile(position);
            Vector2 centerTile = GameWorld.PixelToTile(centerPosition);

            int halfWidth = width / 2;
            int halfHeight = height / 2;

            return tilePos.X >= centerTile.X - halfWidth &&
                   tilePos.X <= centerTile.X + halfWidth &&
                   tilePos.Y >= centerTile.Y - halfHeight &&
                   tilePos.Y <= centerTile.Y + halfHeight;
        }

        public List<Vector2> GetFencePositions()
        {
            return fencePositions;
        }

        public List<Animal> GetAnimals()
        {
            return animals;
        }

        public Vector2 GetCenterPosition()
        {
            return centerPosition;
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (fenceTexture == null) return;

            foreach (Vector2 position in fencePositions)
            {
                spriteBatch.Draw(fenceTexture, position, null, Color.White, 0f, 
                    new Vector2(fenceTexture.Width / 2, fenceTexture.Height / 2), 
                    2f, SpriteEffects.None, 0f);
            }

            // Draw all animals in this habitat
            foreach (var animal in animals)
            {
                animal.Draw(spriteBatch);
            }
        }

        public void Update(GameTime gameTime)
        {
            // Update all animals in this habitat
            foreach (var animal in animals)
            {
                animal.Update(gameTime);
            }
        }

        public void LoadAnimalContent(ContentManager content)
        {
            // Load content for all animals in this habitat
            foreach (var animal in animals)
            {
                animal.LoadContent(content);
            }
        }
    }
} 