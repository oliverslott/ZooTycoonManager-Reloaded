using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace ZooTycoonManager
{
    public class Visitor : ISaveable, ILoadable, IInspectableEntity
    {
        private Texture2D sprite;
        private Texture2D thoughtBubbleTexture;
        private Texture2D animalInThoughtTexture;
        private Vector2 position;
        private List<Node> path;
        private int currentNodeIndex = 0;
        private float speed = 80f;
        private AStarPathfinding pathfinder;
        private Random random = new Random();
        private float timeSinceLastRandomWalk = 0f;
        private const float RANDOM_WALK_INTERVAL = 5f;
        private const float VISIT_DURATION = 4f;
        private float currentVisitTime = 0f;
        private Habitat currentHabitat = null;

        private Thread _updateThread;
        private readonly object _positionLock = new object();
        private bool _isRunning = true;
        private HashSet<int> _visitedHabitatIds;
        private bool _isExiting = false;
        private Vector2 _exitTargetPosition;

        private Vector2 _pathfindingStartPos;
        private Vector2 _pathfindingTargetPos;

        private static Texture2D _borderTexture;

        private const float HUNGER_INCREASE_RATE = 0.2f;
        private float _uncommittedHungerPoints = 0f;

        public bool IsSelected { get; set; }
        int IInspectableEntity.Id => VisitorId;

        // Database
        public int VisitorId { get; set; }
        public string Name { get; set; }
        public int Money { get; set; }
        public int Mood { get; set; }
        public int Hunger { get; set; }
        public int? HabitatId { get; set; }
        public int? ShopId { get; set; }
        private int _positionX;
        private int _positionY;

        public Vector2 Position 
        { 
            get => position;
            private set
            {
                position = value;
                Vector2 tilePos = GameWorld.PixelToTile(value);
                _positionX = (int)tilePos.X;
                _positionY = (int)tilePos.Y;
            }
        }

        public int PositionX => _positionX;
        public int PositionY => _positionY;

        public Rectangle BoundingBox => new Rectangle((int)(Position.X - 16), (int)(Position.Y - 16), 32, 32);

        public Visitor(Vector2 spawnPosition, int visitorId = 0)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            Position = spawnPosition;
            VisitorId = visitorId;
            _visitedHabitatIds = new HashSet<int>();

            Name = "Visitor";
            Money = 100;
            Mood = 100;
            Hunger = 0;
            HabitatId = null;
            ShopId = null;

            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;

            _updateThread = new Thread(UpdateLoop);
            _updateThread.Name = $"Visitor_{GetHashCode()}_Update";
            _updateThread.IsBackground = true;
            _updateThread.Start();
        }

        private void UpdateLoop()
        {
            GameTime gameTime = new GameTime();
            DateTime lastUpdate = DateTime.Now;

            while (_isRunning)
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan elapsed = currentTime - lastUpdate;
                gameTime.ElapsedGameTime = elapsed;
                gameTime.TotalGameTime += elapsed;
                lastUpdate = currentTime;

                Update(gameTime);
                Thread.Sleep(16); // 60 fps
            }
        }

        private void TryRandomWalk(GameTime gameTime)
        {
            if (path != null && path.Count > 0 && currentNodeIndex < path.Count) return;
            if (_isExiting || currentHabitat != null) return;

            timeSinceLastRandomWalk += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeSinceLastRandomWalk >= RANDOM_WALK_INTERVAL)
            {
                timeSinceLastRandomWalk = 0f;
                PerformNextActionDecision();
            }
        }

        private void PerformNextActionDecision()
        {
            if (path != null && path.Count > 0 && currentNodeIndex < path.Count) return;
            if (_isExiting || currentHabitat != null) return;

            var allHabitats = GameWorld.Instance.GetHabitats();

            if (allHabitats.Count > 0)
            {
                var unvisitedHabitats = allHabitats.Where(h => !_visitedHabitatIds.Contains(h.HabitatId)).ToList();

                if (unvisitedHabitats.Count == 0)
                {
                    InitiateExit();
                    return;
                }

                if (random.NextDouble() < 0.7)
                {
                    var randomHabitat = unvisitedHabitats[random.Next(unvisitedHabitats.Count)];
                    List<Vector2> availableSpots = randomHabitat.GetWalkableVisitingSpots();
                    
                    if (availableSpots.Count > 0)
                    {
                        Vector2 visitPosition = availableSpots[random.Next(availableSpots.Count)];
                        if (randomHabitat.TryEnterHabitatSync(this))
                        {
                            PathfindTo(visitPosition);

                            if (path == null || path.Count == 0)
                            {
                                Debug.WriteLine($"Visitor {VisitorId}: Pathfinding to habitat {randomHabitat.HabitatId} ({randomHabitat.Name}) spot {visitPosition} from {Position} failed. Releasing spot.");
                                randomHabitat.LeaveHabitat(this);
                            }
                            else
                            {
                                currentHabitat = randomHabitat;
                                currentVisitTime = 0f;
                                Debug.WriteLine($"Visitor {VisitorId}: Successfully pathfinding to habitat {currentHabitat.HabitatId} ({currentHabitat.Name}) spot {visitPosition} from {Position}. Path length: {path.Count}");
                                return;
                            }
                        }
                    }
                }
            }


            List<Vector2> walkableTiles = GameWorld.Instance.GetWalkableTileCoordinates();

            if (walkableTiles.Count > 0)
            {
                Vector2 randomTilePos = walkableTiles[random.Next(walkableTiles.Count)];
                Vector2 randomPixelPos = GameWorld.TileToPixel(randomTilePos);
                PathfindTo(randomPixelPos);
            }
            else
            {
                Debug.WriteLine($"Visitor {VisitorId}: No walkable tiles found for random walk.");
            }
        }

        public void PathfindTo(Vector2 targetDestination)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            _pathfindingStartPos = position;
            _pathfindingTargetPos = targetDestination;

            Vector2 startTile = GameWorld.PixelToTile(_pathfindingStartPos);
            Vector2 targetTile = GameWorld.PixelToTile(_pathfindingTargetPos);
            
            List<Node> calculatedPath = pathfinder.FindPath(
                (int)startTile.X, (int)startTile.Y,
                (int)targetTile.X, (int)targetTile.Y);

            path = calculatedPath;
            currentNodeIndex = 0;

            if (path != null && path.Count > 0)
            {

            }
            else
            {
                path = null;
            }
        }

        public void LoadContent(ContentManager contentManager)
        {
            sprite = contentManager.Load<Texture2D>("Pawn_Blue_Cropped_resized");
            thoughtBubbleTexture = contentManager.Load<Texture2D>("Thought_bubble");
            animalInThoughtTexture = contentManager.Load<Texture2D>("NibblingGoat");


            if (_borderTexture == null)
            {
                _borderTexture = new Texture2D(GameWorld.Instance.GraphicsDevice, 1, 1);
                _borderTexture.SetData(new[] { Color.White });
            }
        }

        private void Update(GameTime gameTime)
        {
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

            if (!_isExiting && currentHabitat != null)
            {
                if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
                {
                    currentVisitTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (currentVisitTime >= VISIT_DURATION)
                    {
                        if (currentHabitat != null)
                        {
                            _visitedHabitatIds.Add(currentHabitat.HabitatId);
                            currentHabitat.LeaveHabitat(this);
                            currentHabitat = null;
                            currentVisitTime = 0f;
                            if (!_isExiting)
                            {
                                PerformNextActionDecision();
                            }
                        }
                    }
                }
            }


            if (!_isExiting) 
            {
                TryRandomWalk(gameTime);
            }


            if (_isExiting && (path == null || path.Count == 0 || currentNodeIndex >= path.Count))
            {
                _isRunning = false;
                GameWorld.Instance.ConfirmDespawn(this);
                return; 
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
                Vector2 directionToNode = targetNodePosition - position;
                float distanceToNode = directionToNode.Length();

                if (distanceToNode <= remainingMoveThisFrame)
                {
                    lock (_positionLock)
                    {
                        Position = targetNodePosition;
                    }
                    currentNodeIndex++;
                    remainingMoveThisFrame -= distanceToNode;
                }
                else
                {
                    if (distanceToNode > 0)
                    {
                        directionToNode.Normalize();
                        lock (_positionLock)
                        {
                            Position = position + directionToNode * remainingMoveThisFrame;
                        }
                    }
                    remainingMoveThisFrame = 0;
                }
            }

            if (currentNodeIndex >= path.Count) 
            {
                Vector2 pathEndTargetForLog = _pathfindingTargetPos;
                path = null; 
                currentNodeIndex = 0;

                if (_isExiting)
                {
                    _isRunning = false;
                    GameWorld.Instance.ConfirmDespawn(this);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (sprite == null) return;
            lock (_positionLock)
            {
                spriteBatch.Draw(sprite, position, new Rectangle(0, 0, 32, 32), Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);

                if (currentHabitat != null && (path == null || path.Count == 0 || currentNodeIndex >= path.Count) && thoughtBubbleTexture != null && animalInThoughtTexture != null)
                {
                    Vector2 thoughtBubblePosition = new Vector2(position.X, position.Y - sprite.Height); 

                    spriteBatch.Draw(thoughtBubbleTexture, thoughtBubblePosition, null, Color.White, 0f, new Vector2(thoughtBubbleTexture.Width / 2, thoughtBubbleTexture.Height /2), 0.5f, SpriteEffects.None, 0.1f);

                    Vector2 animalTexturePosition = new Vector2(thoughtBubblePosition.X, thoughtBubblePosition.Y - 4);

                    spriteBatch.Draw(animalInThoughtTexture, animalTexturePosition, new Rectangle(0, 0, 16, 16), Color.White, 0f, new Vector2(16 / 2, 16 / 2), 1f, SpriteEffects.None, 0.2f);
                }


                if (IsSelected)
                {
                    DrawBorder(spriteBatch, BoundingBox, 2, Color.Yellow); 
                }
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

        public Vector2 GetPosition()
        {
            lock (_positionLock)
            {
                return position;
            }
        }

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$visitor_id", VisitorId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$money", Money);
            command.Parameters.AddWithValue("$mood", Mood);
            command.Parameters.AddWithValue("$hunger", Hunger);
            command.Parameters.AddWithValue("$habitat_id", (object)HabitatId ?? DBNull.Value);
            command.Parameters.AddWithValue("$shop_id", (object)ShopId ?? DBNull.Value);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Visitor 
                SET name = $name, 
                    money = $money, 
                    mood = $mood, 
                    hunger = $hunger, 
                    habitat_id = $habitat_id, 
                    shop_id = $shop_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE visitor_id = $visitor_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Visitor (visitor_id, name, money, mood, hunger, habitat_id, shop_id, position_x, position_y)
                    VALUES ($visitor_id, $name, $money, $mood, $hunger, $habitat_id, $shop_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
            }
        }

        public void Load(SqliteDataReader reader)
        {
            VisitorId = reader.GetInt32(0);
            Name = reader.GetString(1);
            Money = reader.GetInt32(2);
            Mood = reader.GetInt32(3);
            Hunger = reader.GetInt32(4);
            HabitatId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5);
            ShopId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6);
            int posX = reader.GetInt32(7);
            int posY = reader.GetInt32(8);


            Vector2 pixelPos = GameWorld.TileToPixel(new Vector2(posX, posY));
            Position = pixelPos;

            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            path = null;
            currentNodeIndex = 0;
            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;
            currentVisitTime = 0f;
            _visitedHabitatIds = new HashSet<int>();
            _isExiting = false;
            _uncommittedHungerPoints = 0f;
        }

        public void InitiateExit()
        {
            if (_isExiting) return;

            _isExiting = true;

            _exitTargetPosition = GameWorld.Instance.VisitorExitPosition;
            
            path = null;
            currentNodeIndex = 0;
            if (currentHabitat != null)
            {
                currentHabitat.LeaveHabitat(this);
                currentHabitat = null;
            }
            currentVisitTime = 0f;
            timeSinceLastRandomWalk = float.MinValue;


            PathfindTo(_exitTargetPosition);
        }
    }
}
