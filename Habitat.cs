using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace ZooTycoonManager
{
    public class Habitat: ISaveable, ILoadable
    {
        // Enclosure size constant
        public const int DEFAULT_ENCLOSURE_SIZE = 9;  // Total size of the enclosure (9x9)
        private const int MAX_VISITORS = 3;  // Maximum number of visitors allowed in a habitat

        private Vector2 centerPosition;
        private int width;
        private int height;
        private List<Vector2> fencePositions;
        private List<Animal> animals;
        private static Texture2D fenceTexture;
        private SemaphoreSlim visitorSemaphore;  // Semaphore to control visitor access
        private HashSet<Visitor> currentVisitors;  // Track current visitors

        //Database
        public int HabitatId { get; set; }
        public int Size { get; set; }
        public int MaxAnimals { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        private int positionX;
        private int positionY;

        public Vector2 CenterPosition 
        { 
            get => centerPosition;
            private set
            {
                centerPosition = value;
                // Update database position properties with tile coordinates
                Vector2 tilePos = GameWorld.PixelToTile(value);
                positionX = (int)tilePos.X;
                positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => positionX;
        public int PositionY => positionY;

        public static int GetEnclosureRadius()
        {
            return (DEFAULT_ENCLOSURE_SIZE - 1) / 2;
        }

        public Habitat(Vector2 centerPosition, int width, int height, int habitatId = 0)
        {
            this.width = width;
            this.height = height;
            this.fencePositions = new List<Vector2>();
            this.animals = new List<Animal>();
            this.visitorSemaphore = new SemaphoreSlim(MAX_VISITORS);
            this.currentVisitors = new HashSet<Visitor>();
            CenterPosition = centerPosition;
            HabitatId = habitatId;

            Size = 1;
            MaxAnimals = 10;
            Name = "Goat Habitat";
            Type = "Normal";
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
            Vector2 centerTile = GameWorld.PixelToTile(CenterPosition);

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
            return CenterPosition;
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

        public void PlaceEnclosure(Vector2 centerPixelPosition)
        {
            Debug.WriteLine($"Starting enclosure placement at pixel position: {centerPixelPosition}");
            
            CenterPosition = centerPixelPosition;
            
            // Convert the center position to tile coordinates
            Vector2 centerTile = GameWorld.PixelToTile(centerPixelPosition);
            Debug.WriteLine($"Center tile position: {centerTile}");

            // Calculate the corners of the enclosure
            int radius = GetEnclosureRadius();
            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;

            Debug.WriteLine($"Enclosure bounds: ({startX},{startY}) to ({endX},{endY})");

            // Place the top and bottom rows
            for (int x = startX; x <= endX; x++)
            {
                PlaceFenceTile(new Vector2(x, startY)); // Top row
                PlaceFenceTile(new Vector2(x, endY));   // Bottom row
            }

            // Place the left and right columns (excluding corners which are already placed)
            for (int y = startY + 1; y < endY; y++)
            {
                PlaceFenceTile(new Vector2(startX, y)); // Left column
                PlaceFenceTile(new Vector2(endX, y));   // Right column
            }
        }

        private void PlaceFenceTile(Vector2 tilePos)
        {
            // Ensure we're within bounds
            if (tilePos.X < 0 || tilePos.X >= GameWorld.GRID_WIDTH ||
                tilePos.Y < 0 || tilePos.Y >= GameWorld.GRID_HEIGHT)
            {
                Debug.WriteLine($"Skipping out of bounds fence at: {tilePos}");
                return;
            }

            Vector2 pixelPos = GameWorld.TileToPixel(tilePos);
            Debug.WriteLine($"Attempting to place fence at tile: {tilePos}, pixel: {pixelPos}");
            
            GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y] = false;
            AddFencePosition(pixelPos);
            Debug.WriteLine($"Successfully placed fence at: {tilePos}");
        }

        public bool SpawnAnimal(Vector2 pixelPosition)
        {
            Vector2 tilePos = GameWorld.PixelToTile(pixelPosition);
            
            // Only spawn if the position is walkable and within bounds
            if (tilePos.X >= 0 && tilePos.X < GameWorld.GRID_WIDTH && 
                tilePos.Y >= 0 && tilePos.Y < GameWorld.GRID_HEIGHT &&
                GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y])
            {
                Vector2 spawnPos = GameWorld.TileToPixel(tilePos);
                
                Animal newAnimal = new Animal(GameWorld.Instance.GetNextAnimalId());
                newAnimal.SetPosition(spawnPos);
                newAnimal.LoadContent(GameWorld.Instance.Content);
                newAnimal.SetHabitat(this);
                AddAnimal(newAnimal);
                return true;
            }
            return false;
        }

        public Vector2? GetRandomFencePosition()
        {
            if (fencePositions.Count == 0) return null;

            // Get visitor's current position from GameWorld
            var visitors = GameWorld.Instance.GetVisitors();
            if (visitors.Count == 0) return null;

            // Find the nearest visitor
            Vector2? nearestPosition = null;
            float shortestDistance = float.MaxValue;
            Vector2? nearestFencePos = null;

            foreach (var visitor in visitors)
            {
                Vector2 visitorPos = visitor.GetPosition();
                foreach (var fencePos in fencePositions)
                {
                    float distance = Vector2.Distance(visitorPos, fencePos);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        nearestFencePos = fencePos;
                    }
                }
            }

            if (!nearestFencePos.HasValue) return null;

            // Convert to tile position
            Vector2 tilePos = GameWorld.PixelToTile(nearestFencePos.Value);
            
            // Try to find a walkable position adjacent to the fence
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            
            // Check each adjacent position
            Vector2? nearestWalkablePos = null;
            float nearestWalkableDistance = float.MaxValue;

            foreach (var visitor in visitors)
            {
                Vector2 visitorPos = visitor.GetPosition();
                for (int i = 0; i < 4; i++)
                {
                    int newX = (int)tilePos.X + dx[i];
                    int newY = (int)tilePos.Y + dy[i];
                    
                    if (newX >= 0 && newX < GameWorld.GRID_WIDTH &&
                        newY >= 0 && newY < GameWorld.GRID_HEIGHT &&
                        GameWorld.Instance.WalkableMap[newX, newY])
                    {
                        Vector2 walkablePos = GameWorld.TileToPixel(new Vector2(newX, newY));
                        float distance = Vector2.Distance(visitorPos, walkablePos);
                        if (distance < nearestWalkableDistance)
                        {
                            nearestWalkableDistance = distance;
                            nearestWalkablePos = walkablePos;
                        }
                    }
                }
            }

            return nearestWalkablePos;
        }

        public async Task<bool> TryEnterHabitat(Visitor visitor)
        {
            if (await visitorSemaphore.WaitAsync(0))  // Try to acquire the semaphore without waiting
            {
                lock (currentVisitors)
                {
                    currentVisitors.Add(visitor);
                }
                return true;
            }
            return false;
        }

        public bool TryEnterHabitatSync(Visitor visitor)
        {
            if (visitorSemaphore.Wait(0))  // Try to acquire the semaphore without waiting
            {
                lock (currentVisitors)
                {
                    currentVisitors.Add(visitor);
                }
                return true;
            }
            return false;
        }

        public void LeaveHabitat(Visitor visitor)
        {
            lock (currentVisitors)
            {
                if (currentVisitors.Remove(visitor))
                {
                    visitorSemaphore.Release();
                }
            }
        }

        public int GetCurrentVisitorCount()
        {
            lock (currentVisitors)
            {
                return currentVisitors.Count;
            }
        }

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$habitat_id", HabitatId);
            command.Parameters.AddWithValue("$size", Size);
            command.Parameters.AddWithValue("$max_animals", MaxAnimals);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$type", Type);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            try
            {
                // Try to insert first
                command.CommandText = @"
                    INSERT INTO Habitat (habitat_id, size, max_animals, name, type, position_x, position_y)
                    VALUES ($habitat_id, $size, $max_animals, $name, $type, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Habitat: ID {HabitatId}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                // If insert fails due to primary key constraint, update instead
                command.CommandText = @"
                    UPDATE Habitat 
                    SET size = $size, 
                        max_animals = $max_animals, 
                        name = $name, 
                        type = $type, 
                        position_x = $position_x, 
                        position_y = $position_y
                    WHERE habitat_id = $habitat_id;
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Updated Habitat: ID {HabitatId}");
            }
        }

        public void Load(SqliteDataReader reader)
        {
            HabitatId = reader.GetInt32(0);
            Size = reader.GetInt32(1);
            MaxAnimals = reader.GetInt32(2);
            Name = reader.GetString(3);
            Type = reader.GetString(4);
            int posX = reader.GetInt32(5);
            int posY = reader.GetInt32(6);

            // Convert tile position to pixel position
            Vector2 pixelPos = GameWorld.TileToPixel(new Vector2(posX, posY));
            CenterPosition = pixelPos;
            
            // Initialize other properties
            width = Habitat.DEFAULT_ENCLOSURE_SIZE;
            height = Habitat.DEFAULT_ENCLOSURE_SIZE;
            fencePositions = new List<Vector2>();
            animals = new List<Animal>();
            visitorSemaphore = new SemaphoreSlim(MAX_VISITORS);
            currentVisitors = new HashSet<Visitor>();

            // Place the enclosure
            PlaceEnclosure(pixelPos);
        }
    }
} 