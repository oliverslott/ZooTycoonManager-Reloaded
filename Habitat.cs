using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace ZooTycoonManager
{
    public enum HabitatSizeType
    {
        Small,
        Medium,
        Large
    }

    public class Habitat: ISaveable, ILoadable
    {
        private const int MAX_VISITORS = 3;
        private const float FENCE_DRAW_SCALE = 2.67f;

        private Vector2 centerPosition;
        private int width;
        private int height;
        private List<Vector2> fencePositions;
        private List<Animal> animals;
        private HashSet<Vector2> fenceTileCoordinates;
        private SemaphoreSlim visitorSemaphore;
        private HashSet<Visitor> currentVisitors;
        private List<Zookeeper> zookeepers;
        
        //Database
        public int HabitatId { get; set; }
        public int MaxAnimals { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        private int positionX;
        private int positionY;

        public HabitatSizeType CurrentSizeType { get; private set; }

        public int MaxAnimalsBeforeStress
        {
            get
            {
                switch (CurrentSizeType)
                {
                    case HabitatSizeType.Small: return 3;
                    case HabitatSizeType.Medium: return 5;
                    case HabitatSizeType.Large: return 8;
                    default: return 5;
                }
            }
        }

        public Vector2 CenterPosition 
        { 
            get => centerPosition;
            private set
            {
                centerPosition = value;
                Vector2 tilePos = GameWorld.PixelToTile(value);
                positionX = (int)tilePos.X;
                positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => positionX;
        public int PositionY => positionY;

        public int GetEnclosureRadius()
        {
            switch (CurrentSizeType)
            {
                case HabitatSizeType.Small: return 2;
                case HabitatSizeType.Medium: return 4;
                case HabitatSizeType.Large: return 6;
                default: return 4;
            }
        }

        public Habitat(Vector2 centerPosition, HabitatSizeType sizeType, int habitatId)
        {
            this.animals = new List<Animal>();
            this.zookeepers = new List<Zookeeper>();
            this.visitorSemaphore = new SemaphoreSlim(MAX_VISITORS);
            this.currentVisitors = new HashSet<Visitor>();
            this.fencePositions = new List<Vector2>();
            this.fenceTileCoordinates = new HashSet<Vector2>();

            this.HabitatId = habitatId;
            this.CurrentSizeType = sizeType;
            
            this.width = (GetEnclosureRadius() * 2) + 1;
            this.height = (GetEnclosureRadius() * 2) + 1;
            
            this.MaxAnimals = 10;
            this.Name = $"{sizeType} Habitat";
            this.Type = "Normal";

            this.CenterPosition = centerPosition;

            PlaceEnclosure(this.CenterPosition);
        }

        public Habitat()
        {
            this.animals = new List<Animal>();
            this.zookeepers = new List<Zookeeper>();
            this.visitorSemaphore = new SemaphoreSlim(MAX_VISITORS);
            this.currentVisitors = new HashSet<Visitor>();
            this.fencePositions = new List<Vector2>();
            this.fenceTileCoordinates = new HashSet<Vector2>();
        }

        public static void LoadContent(ContentManager content)
        {

        }

        public void AddFencePosition(Vector2 position)
        {
            fencePositions.Add(position);
        }

        public void AddAnimal(Animal animal)
        {
            animals.Add(animal);
        }

        public void AddZookeeper(Zookeeper zookeeper)
        {
            zookeepers.Add(zookeeper);
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

        public List<Zookeeper> GetZookeepers()
        {
            return zookeepers; 
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
            FenceRenderer.Draw(spriteBatch, fencePositions, fenceTileCoordinates, FENCE_DRAW_SCALE);

            foreach (var animal in animals)
            {
                animal.Draw(spriteBatch);
            }

            foreach (var zookeeper in zookeepers)
            {
                zookeeper.Draw(spriteBatch);
            }
        }

        public void Update(GameTime gameTime)
        {
            foreach (var animal in animals)
            {
                animal.Update(gameTime);
            }
        }

        public void LoadAnimalContent(ContentManager content)
        {
            foreach (var animal in animals)
            {
                animal.LoadContent(content);
            }

            foreach (var zookeeper in zookeepers)
            {
                zookeeper.LoadContent(content);
            }
        }

        public void PlaceEnclosure(Vector2 centerPixelPosition)
        {            
            CenterPosition = centerPixelPosition;
            fencePositions.Clear(); 
            fenceTileCoordinates.Clear(); 
            
            Vector2 centerTile = GameWorld.PixelToTile(centerPixelPosition);

            int radius = GetEnclosureRadius();
            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;

            for (int x = startX; x <= endX; x++)
            {
                PlaceFenceTile(new Vector2(x, startY));
                PlaceFenceTile(new Vector2(x, endY)); 
            }

            for (int y = startY + 1; y < endY; y++)
            {
                PlaceFenceTile(new Vector2(startX, y));
                PlaceFenceTile(new Vector2(endX, y)); 
            }

            for (int x = startX + 1; x < endX; x++)
            {
                for (int y = startY + 1; y < endY; y++)
                {
                    if (x >= 0 && x < GameWorld.GRID_WIDTH && y >= 0 && y < GameWorld.GRID_HEIGHT)
                    {
                        GameWorld.Instance.WalkableMap[x, y] = true;
                    }
                }
            }
        }

        public void RemoveEnclosure()
        {
            Vector2 centerTile = GameWorld.PixelToTile(CenterPosition);
            int radius = GetEnclosureRadius();
            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (x >= 0 && x < GameWorld.GRID_WIDTH && y >= 0 && y < GameWorld.GRID_HEIGHT)
                    {
                        GameWorld.Instance.WalkableMap[x, y] = GameWorld.Instance.GetOriginalWalkableState(x, y);
                    }
                }
            }

            fencePositions.Clear();
            fenceTileCoordinates.Clear();
        }

        private void PlaceFenceTile(Vector2 tilePos)
        {
            if (tilePos.X < 0 || tilePos.X >= GameWorld.GRID_WIDTH ||
                tilePos.Y < 0 || tilePos.Y >= GameWorld.GRID_HEIGHT)
            {
                Debug.WriteLine($"Skipping out of bounds fence at: {tilePos}");
                return;
            }

            GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y] = false;
            AddFencePosition(tilePos);
            fenceTileCoordinates.Add(tilePos);
        }

        public bool SpawnAnimal(Vector2 pixelPosition)
        {
            Vector2 tilePos = GameWorld.PixelToTile(pixelPosition);
            decimal animalCost = 1000;

            if (tilePos.X >= 0 && tilePos.X < GameWorld.GRID_WIDTH && 
                tilePos.Y >= 0 && tilePos.Y < GameWorld.GRID_HEIGHT &&
                GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y])
            {
                if (MoneyManager.Instance.SpendMoney(animalCost))
                {
                    Vector2 spawnPos = GameWorld.TileToPixel(tilePos);
                    
                    Animal newAnimal = new Animal(GameWorld.Instance.GetNextAnimalId());
                    newAnimal.SetPosition(spawnPos);
                    newAnimal.LoadContent(GameWorld.Instance.Content);
                    newAnimal.SetHabitat(this);
                    AddAnimal(newAnimal);
                    return true;
                }
                else
                {
                    Debug.WriteLine("Not enough money to spawn an animal.");
                    return false;
                }
            }
            return false;
        }

        public List<Vector2> GetWalkableVisitingSpots()
        {
            HashSet<Vector2> visitingSpots = new HashSet<Vector2>();
            if (fencePositions == null || fencePositions.Count == 0)
            {
                return visitingSpots.ToList();
            }

            foreach (Vector2 fenceTilePos in fencePositions)
            {
                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };

                for (int i = 0; i < 4; i++)
                {
                    int adjacentTileX = (int)fenceTilePos.X + dx[i];
                    int adjacentTileY = (int)fenceTilePos.Y + dy[i];

                    if (adjacentTileX >= 0 && adjacentTileX < GameWorld.GRID_WIDTH &&
                        adjacentTileY >= 0 && adjacentTileY < GameWorld.GRID_HEIGHT)
                    {
                        Vector2 adjacentTile = new Vector2(adjacentTileX, adjacentTileY);
                        if (GameWorld.Instance.WalkableMap[adjacentTileX, adjacentTileY] &&
                            !fenceTileCoordinates.Contains(adjacentTile) && 
                            !this.ContainsPosition(GameWorld.TileToPixel(adjacentTile)))
                        {
                            visitingSpots.Add(adjacentTile);
                        }
                    }
                }
            }
            return visitingSpots.ToList();
        }

        public bool TryEnterHabitatSync(Visitor visitor)
        {
            if (visitorSemaphore.Wait(0))
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
                command.CommandText = @"
                    INSERT INTO Habitat (habitat_id, size, max_animals, name, type, position_x, position_y)
                    VALUES ($habitat_id, $size, $max_animals, $name, $type, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Habitat: ID {HabitatId}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
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
            CurrentSizeType = (HabitatSizeType)reader.GetInt32(1);
            MaxAnimals = reader.GetInt32(2);
            Name = reader.GetString(3);
            Type = reader.GetString(4);
            int posX = reader.GetInt32(5);
            int posY = reader.GetInt32(6);

            Vector2 pixelPos = GameWorld.TileToPixel(new Vector2(posX, posY));
            CenterPosition = pixelPos;
            
            width = (GetEnclosureRadius() * 2) + 1;
            height = (GetEnclosureRadius() * 2) + 1;
            fencePositions = new List<Vector2>();
            animals = new List<Animal>();
            visitorSemaphore = new SemaphoreSlim(MAX_VISITORS);
            currentVisitors = new HashSet<Visitor>();

            PlaceEnclosure(pixelPos);
        }

        public int Size
        {
            get { return (int)CurrentSizeType; }
        }
    }
} 