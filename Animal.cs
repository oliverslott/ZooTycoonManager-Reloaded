using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ZooTycoonManager
{
    public class Animal: ISaveable, ILoadable, IInspectableEntity, IStressableEntity
    {
        Texture2D sprite;
        List<Node> path;
        int currentNodeIndex = 0;
        float speed = 100f;
        AStarPathfinding pathfinder;
        private Habitat currentHabitat;
        private Random random = new Random();
        private float timeSinceLastRandomWalk = 0f;
        private const float RANDOM_WALK_INTERVAL = 3f;

        private const float HUNGER_INCREASE_RATE = 0.5f;
        private float _uncommittedHungerPoints = 0f;

        private const float STRESS_INCREASE_RATE_OVERCROWDING = 25.0f;
        private float _uncommittedStressPoints = 0f;

        private Thread _pathfindingWorkerThread;
        private readonly AutoResetEvent _pathfindingRequestEvent = new AutoResetEvent(false);
        private volatile bool _workerThreadRunning = true;
        private Vector2 _requestedPathfindingStartPos;
        private Vector2 _requestedPathfindingTargetPos;
        private List<Node> _pendingPathResult;
        private readonly object _pendingPathResultLock = new object();

        private ThoughtBubble _thoughtBubble;
        private Texture2D _drumstickTexture;

        public bool IsPathfinding { get; private set; }
        public bool IsSelected { get; set; }
        private static Texture2D _borderTexture;

        //Database
        public int AnimalId { get; set; }
        public string Name { get; set; }
        public int Mood { get; set; }
        public int Hunger { get; set; }
        public int Stress { get; set; }
        public int HabitatId { get; set; }

        int IInspectableEntity.Id => AnimalId;
        string IInspectableEntity.Name => Name;
        int IInspectableEntity.Mood => Mood;
        int IInspectableEntity.Hunger => Hunger;
        int IStressableEntity.Stress => Stress;

        private Vector2 _position;
        private int _positionX;
        private int _positionY;

        string species;

        public Vector2 Position 
        { 
            get => _position;
            private set
            {
                _position = value;
                Vector2 tilePos = GameWorld.PixelToTile(value);
                _positionX = (int)tilePos.X;
                _positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => _positionX;
        public int PositionY => _positionY;

        public Rectangle BoundingBox => new Rectangle((int)(Position.X - 8 * 2), (int)(Position.Y - 8 * 2), 16 * 2, 16 * 2);

        public Animal(int animalId = 0, string species = "Buffalo")
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            Position = new Vector2(GameWorld.TILE_SIZE * 5, GameWorld.TILE_SIZE * 5);
            AnimalId = animalId;
            this.species = species;
             
            Name = species;
            Mood = 100;
            Hunger = 0;
            Stress = 0;
            HabitatId = -1;   

            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;
            _uncommittedHungerPoints = 0f;
            _uncommittedStressPoints = 0f;

            _pathfindingWorkerThread = new Thread(PathfindingWorkerLoop);
            _pathfindingWorkerThread.Name = $"Animal_{GetHashCode()}_PathWorker";
            _pathfindingWorkerThread.IsBackground = true;
            _pathfindingWorkerThread.Start();
        }

        public void SetHabitat(Habitat habitat)
        {
            currentHabitat = habitat;
            HabitatId = habitat.HabitatId;
            path = null;
            currentNodeIndex = 0;
            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;
            _uncommittedHungerPoints = 0f;
            _uncommittedStressPoints = 0f;
        }

        private void TryRandomWalk(GameTime gameTime)
        {
            if (currentHabitat == null || IsPathfinding) return;

            timeSinceLastRandomWalk += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeSinceLastRandomWalk >= RANDOM_WALK_INTERVAL)
            {
                timeSinceLastRandomWalk = 0f;                

                Vector2 centerTile = GameWorld.PixelToTile(currentHabitat.GetCenterPosition());
                int halfWidth = (currentHabitat.GetWidth() - 1) / 2;
                int halfHeight = (currentHabitat.GetHeight() - 1) / 2;

                int randomX = random.Next((int)centerTile.X - halfWidth, (int)centerTile.X + halfWidth + 1);
                int randomY = random.Next((int)centerTile.Y - halfHeight, (int)centerTile.Y + halfHeight + 1);

                Vector2 randomTilePos = new Vector2(randomX, randomY);
                Vector2 randomPixelPos = GameWorld.TileToPixel(randomTilePos);

                if (randomX >= 0 && randomX < GameWorld.GRID_WIDTH && 
                    randomY >= 0 && randomY < GameWorld.GRID_HEIGHT &&
                    GameWorld.Instance.WalkableMap[randomX, randomY])
                {
                    PathfindTo(randomPixelPos);
                }
            }
        }

        public void SetPosition(Vector2 newPosition)
        {
            Position = newPosition;
        }

        private void PathfindingWorkerLoop()
        {
            while (_workerThreadRunning)
            {
                _pathfindingRequestEvent.WaitOne();
                if (!_workerThreadRunning) break;

                List<Node> calculatedPath = null;
                Stopwatch stopwatch = new Stopwatch();
                try
                {
                    stopwatch.Start();
                    Vector2 startTile = GameWorld.PixelToTile(_requestedPathfindingStartPos);
                    Vector2 targetTile = GameWorld.PixelToTile(_requestedPathfindingTargetPos);
                    

                    calculatedPath = pathfinder.FindPath(
                        (int)startTile.X, (int)startTile.Y,
                        (int)targetTile.X, (int)targetTile.Y);

                    stopwatch.Stop();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in animal pathfinding worker thread ({Thread.CurrentThread.Name}): {ex.Message}");
                }
                finally
                {
                    lock (_pendingPathResultLock)
                    {
                        _pendingPathResult = calculatedPath;
                    }
                }
            }
            Debug.WriteLine($"Animal Pathfinding Worker ({Thread.CurrentThread.Name}) exiting.");
        }

        public void PathfindTo(Vector2 targetDestination)
        {
            if (IsPathfinding)
            {
                Debug.WriteLine("Animal is already pathfinding or a path request is pending. New request ignored.");
                return;
            }


            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            IsPathfinding = true;
            _requestedPathfindingStartPos = Position;
            _requestedPathfindingTargetPos = targetDestination;

            lock (_pendingPathResultLock)
            {
                _pendingPathResult = null;
            }
            
            _pathfindingRequestEvent.Set();
        }

        public void LoadContent(ContentManager contentManager)
        {
            _drumstickTexture = contentManager.Load<Texture2D>("drumstick");
            _thoughtBubble = new ThoughtBubble();
            _thoughtBubble.LoadContent(contentManager);
            if (_borderTexture == null)
            {
                _borderTexture = new Texture2D(GameWorld.Instance.GraphicsDevice, 1, 1);
                _borderTexture.SetData(new[] { Color.White });
            }
            switch (species)
            {
                case "Buffalo":
                    sprite = contentManager.Load<Texture2D>("EnragedBuffalo");
                    break;
                case "Orangutan":
                    sprite = contentManager.Load<Texture2D>("AgitatedOrangutan");
                    break;
                case "Kangaroo":
                    sprite = contentManager.Load<Texture2D>("HoppingKangaroo");
                    break;
                case "Elephant":
                    sprite = contentManager.Load<Texture2D>("StompingElephant");
                    break;
                case "Polarbear":
                    sprite = contentManager.Load<Texture2D>("PolarBear");
                    break;
                case "Turtle":
                    sprite = contentManager.Load<Texture2D>("SlowTurtle");
                    break;
                case "Camel":
                    sprite = contentManager.Load<Texture2D>("ThirstyCamel");
                    break;
                case "Bear":
                    sprite = contentManager.Load<Texture2D>("KodiakBear");
                    break;
                case "Wolf":
                    sprite = contentManager.Load<Texture2D>("ArcticWolf");
                    break;
                case "Chimpanze":
                    sprite = contentManager.Load<Texture2D>("MindfulChimpanze");
                    break;
            }
        }

        public void Update(GameTime gameTime)
        {
            TryRandomWalk(gameTime);

            // Overcrowding Stress Update
            if (currentHabitat != null)
            {
                if (currentHabitat.GetAnimals().Count > 5)
                {
                    _uncommittedStressPoints += STRESS_INCREASE_RATE_OVERCROWDING * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
                else 
                {
                    _uncommittedStressPoints -= STRESS_INCREASE_RATE_OVERCROWDING * (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
            }

            if (_uncommittedStressPoints >= 1.0f)
            {
                int wholeStressToAdd = (int)_uncommittedStressPoints;
                Stress += wholeStressToAdd;
                if (Stress > 100)
                {
                    Stress = 100;
                }
                _uncommittedStressPoints -= wholeStressToAdd;
            }
            else if (_uncommittedStressPoints <= -1.0f)
            {
                int wholeStressToRemove = (int)Math.Abs(_uncommittedStressPoints);
                Stress -= wholeStressToRemove;
                if (Stress < 0)
                {
                    Stress = 0;
                }
                _uncommittedStressPoints += wholeStressToRemove;
            }

            // Hunger Update
            _uncommittedHungerPoints += HUNGER_INCREASE_RATE * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_uncommittedHungerPoints >= 1.0f)
            {
                int wholePointsToAdd = (int)_uncommittedHungerPoints;
                Hunger += wholePointsToAdd;
                if (Hunger > 100)
                {
                    Hunger = 100;
                }
                _uncommittedHungerPoints -= wholePointsToAdd;
            }

            // Calculate Mood based on Hunger and Stress
            float calculatedMood = 100f - (Hunger * 0.5f) - (Stress * 0.5f);
            Mood = (int)Math.Max(0, Math.Min(100, calculatedMood));

            if (IsPathfinding) 
            {
                bool pathProcessed = false;
                lock (_pendingPathResultLock)
                {
                    if (_pendingPathResult != null)
                    {
                        path = _pendingPathResult;
                        currentNodeIndex = 0;
                        _pendingPathResult = null;
                        pathProcessed = true;
                    }
                }

                if (pathProcessed)
                {
                    IsPathfinding = false;
                }
            }

            if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
            {
                return;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float remainingMoveThisFrame = speed * deltaTime;

            while (remainingMoveThisFrame > 0 && currentNodeIndex < path.Count)
            {
                Node targetNode = path[currentNodeIndex];
                Vector2 targetNodePosition = GameWorld.TileToPixel(new Vector2(targetNode.X, targetNode.Y));
                Vector2 directionToNode = targetNodePosition - Position;
                float distanceToNode = directionToNode.Length();

                if (distanceToNode <= remainingMoveThisFrame)
                {
                    Position = targetNodePosition;
                    currentNodeIndex++;
                    remainingMoveThisFrame -= distanceToNode;
                }
                else
                {
                    if (distanceToNode > 0)
                    {
                        directionToNode.Normalize();
                        Vector2 newPosition = Position + directionToNode * remainingMoveThisFrame;
                        Position = newPosition;
                    }
                    remainingMoveThisFrame = 0;
                }
            }

            if (currentNodeIndex >= path.Count)
            {
                path = null;
                currentNodeIndex = 0;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (sprite == null) return;
            spriteBatch.Draw(sprite, Position, new Rectangle(0, 0, 16, 16), Color.White, 0f, new Vector2(8, 8), 2f, SpriteEffects.None, 0f);

            if (Hunger > 50 && _thoughtBubble != null && _drumstickTexture != null)
            {
                _thoughtBubble.Draw(spriteBatch, Position, sprite.Height * 2, _drumstickTexture, null, 0.3f);
            }

            if (IsSelected)
            {
                DrawBorder(spriteBatch, BoundingBox, 2, Color.Yellow);
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangleToBorder, int thicknessOfBorder, Color borderColor)
        {
            if (_borderTexture == null) return;

            spriteBatch.Draw(_borderTexture, new Rectangle(rectangleToBorder.X, rectangleToBorder.Y, rectangleToBorder.Width, thicknessOfBorder), borderColor);
            spriteBatch.Draw(_borderTexture, new Rectangle(rectangleToBorder.X, rectangleToBorder.Y, thicknessOfBorder, rectangleToBorder.Height), borderColor);
            spriteBatch.Draw(_borderTexture, new Rectangle((rectangleToBorder.X + rectangleToBorder.Width - thicknessOfBorder), rectangleToBorder.Y, thicknessOfBorder, rectangleToBorder.Height), borderColor);
            spriteBatch.Draw(_borderTexture, new Rectangle(rectangleToBorder.X, rectangleToBorder.Y + rectangleToBorder.Height - thicknessOfBorder, rectangleToBorder.Width, thicknessOfBorder), borderColor);
        }

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$animal_id", AnimalId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$mood", Mood);
            command.Parameters.AddWithValue("$hunger", Hunger);
            command.Parameters.AddWithValue("$stress", Stress);
            command.Parameters.AddWithValue("$habitat_id", HabitatId);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Animal 
                SET name = $name, 
                    mood = $mood, 
                    hunger = $hunger, 
                    stress = $stress, 
                    habitat_id = $habitat_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE animal_id = $animal_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Animal (animal_id, name, mood, hunger, stress, habitat_id, position_x, position_y)
                    VALUES ($animal_id, $name, $mood, $hunger, $stress, $habitat_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Animal: ID {AnimalId}, Name: {Name}");
            }
            else
            {
                Debug.WriteLine($"Updated Animal: ID {AnimalId}, Name: {Name}");
            }
        }

        public void Load(SqliteDataReader reader)
        {
            AnimalId = reader.GetInt32(0);
            Name = reader.GetString(1);
            Mood = reader.GetInt32(2);
            Hunger = reader.GetInt32(3);
            Stress = reader.GetInt32(4);
            HabitatId = reader.GetInt32(5);
            int posX = reader.GetInt32(6);
            int posY = reader.GetInt32(7);

            Vector2 pixelPos = GameWorld.TileToPixel(new Vector2(posX, posY));
            Position = pixelPos;

            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            IsPathfinding = false;
            path = null;
            currentNodeIndex = 0;
            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;
            _uncommittedHungerPoints = 0f;
            _uncommittedStressPoints = 0f;
        }
    }
}
