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
        private ThoughtBubble _thoughtBubble;
        private Texture2D _animalInThoughtTexture;
        private Texture2D _sadTexture;
        private Texture2D _drumstickTexture;
        private Vector2 position;
        private List<Node> path;
        private int currentNodeIndex = 0;
        private float speed = 40f;
        private AStarPathfinding pathfinder;
        private Random random = new Random();
        private float timeSinceLastRandomWalk = 0f;
        private const float RANDOM_WALK_INTERVAL = 5f;
        private const float VISIT_DURATION = 4f;
        private float currentVisitTime = 0f;
        private Habitat currentHabitat = null;
        private Shop currentShop = null;

        private float _showSadThoughtBubbleTimer = 0f;
        private const float SAD_BUBBLE_DURATION = 2f;

        private readonly object _positionLock = new object();
        private bool _isRunning = true;
        private HashSet<int> _visitedHabitatIds;
        private bool _isExiting = false;

        private Vector2 _pathfindingStartPos;

        private static Texture2D _borderTexture;

        private const float HUNGER_INCREASE_RATE = 0.5f;
        private const int HUNGER_PRIORITY_THRESHOLD = 50;
        private float _uncommittedHungerPoints = 0f;

        private const int HIGH_HUNGER_THRESHOLD = 80;
        private const float MOOD_PENALTY_PER_SECOND_HIGH_HUNGER = 1.0f;
        private const float MOOD_RECOVERY_PER_SECOND_NOT_HUNGRY = 0.5f;
        private const int MOOD_INFLUENCE_ON_SCORE = 3;
        private float _uncommittedMoodChangePoints = 0f;

        public bool IsSelected { get; set; }
        int IInspectableEntity.Id => VisitorId;
        string IInspectableEntity.Name => Name;
        int IInspectableEntity.Mood => Mood;
        int IInspectableEntity.Hunger => Hunger;
        int IInspectableEntity.SpeciesId => 0;

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

        public Visitor(Vector2 spawnTilePosition, int visitorId = 0)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            
            Position = GameWorld.TileToPixel(spawnTilePosition);
            
            VisitorId = visitorId;
            _visitedHabitatIds = new HashSet<int>();

            Name = "Visitor";
            Money = 100;
            Mood = 100;
            Hunger = 0;
            HabitatId = null;
            ShopId = null;

            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;

            Thread updateThread = new Thread(() =>
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
            });
            updateThread.Name = $"Visitor_{GetHashCode()}_Update";
            updateThread.IsBackground = true;
            updateThread.Start();
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
            if (currentHabitat != null) return;

            if (Hunger > HUNGER_PRIORITY_THRESHOLD && TryVisitShop()) return;

            if (TryVisitHabitat()) return;

            if (Hunger <= HUNGER_PRIORITY_THRESHOLD && TryVisitShop()) return;

            if (path == null || path.Count == 0)
            {
                PerformRandomWalk();
            }
        }

        private bool TryVisitHabitat()
        {
            var allHabitats = GameWorld.Instance.GetHabitats();
            if (allHabitats.Count == 0) return false;

            var unvisitedHabitats = allHabitats.Where(h => !_visitedHabitatIds.Contains(h.HabitatId)).ToList();

            if (unvisitedHabitats.Count == 0)
            {
                InitiateExit();
                return (path != null && path.Count > 0);
            }

            if (random.NextDouble() < 0.7)
            {
                var randomHabitat = unvisitedHabitats[random.Next(unvisitedHabitats.Count)];
                List<Vector2> availableSpots = randomHabitat.GetWalkableVisitingSpots();

                if (availableSpots.Count > 0)
                {
                    Vector2 visitTilePosition = availableSpots[random.Next(availableSpots.Count)];
                    if (randomHabitat.TryEnterHabitatSync(this))
                    {
                        PathfindTo(visitTilePosition);

                        if (path == null || path.Count == 0)
                        {
                            Debug.WriteLine($"Visitor {VisitorId}: Pathfinding to habitat {randomHabitat.HabitatId} ({randomHabitat.Name}) spot (tile: {visitTilePosition}) from (pixel: {Position}) failed. Releasing spot.");
                            randomHabitat.LeaveHabitat(this);
                            return false;
                        }
                        else
                        {
                            currentHabitat = randomHabitat;
                            currentVisitTime = 0f;
                            Debug.WriteLine($"Visitor {VisitorId}: Successfully pathfinding to habitat {currentHabitat.HabitatId} ({currentHabitat.Name}) spot (tile: {visitTilePosition}) from (pixel: {Position}). Path length: {path.Count}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool TryVisitShop()
        {
            var allShops = GameWorld.Instance.GetShops();
            if (allShops == null || allShops.Count == 0) return false;

            var availableShops = allShops.Where(s => s.GetWalkableVisitingSpots().Count > 0).ToList();
            if (availableShops.Count == 0) return false;

            if (random.NextDouble() < 0.5)
            {
                Shop randomShop = availableShops[random.Next(availableShops.Count)];
                List<Vector2> availableSpots = randomShop.GetWalkableVisitingSpots();

                if (availableSpots.Count > 0)
                {
                    Vector2 visitTilePosition = availableSpots[random.Next(availableSpots.Count)];
                    if (randomShop.TryEnterShopSync(this))
                    {
                        PathfindTo(visitTilePosition);

                        if (path == null || path.Count == 0)
                        {
                            Debug.WriteLine($"Visitor {VisitorId}: Pathfinding to shop {randomShop.ShopId} spot (tile: {visitTilePosition}) from (pixel: {Position}) failed. Releasing spot.");
                            randomShop.LeaveShop(this);
                            return false;
                        }
                        else
                        {
                            currentShop = randomShop;
                            currentVisitTime = 0f;
                            Debug.WriteLine($"Visitor {VisitorId}: Successfully pathfinding to shop {currentShop.ShopId} spot (tile: {visitTilePosition}). Path length: {path.Count}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void PerformRandomWalk()
        {
            List<Vector2> walkableTiles = GameWorld.Instance.GetWalkableTileCoordinates();

            if (walkableTiles.Count > 0)
            {
                Vector2 randomTilePos = walkableTiles[random.Next(walkableTiles.Count)];
                PathfindTo(randomTilePos);
                Debug.WriteLine($"Visitor {VisitorId}: Performing random walk to {randomTilePos}.");
            }
            else
            {
                Debug.WriteLine($"Visitor {VisitorId}: No walkable tiles found for random walk.");
            }
        }

        public void PathfindTo(Vector2 targetTileDestination)
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);

            _pathfindingStartPos = position;

            Vector2 startTile = GameWorld.PixelToTile(_pathfindingStartPos);
            
            List<Node> calculatedPath = pathfinder.FindPath(
                (int)startTile.X, (int)startTile.Y,
                (int)targetTileDestination.X, (int)targetTileDestination.Y);

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
            sprite = contentManager.Load<Texture2D>("294f5329-d985-4d20-86d5-98e9dfb256fc");
            
            _thoughtBubble = new ThoughtBubble();
            _thoughtBubble.LoadContent(contentManager);
            _animalInThoughtTexture = contentManager.Load<Texture2D>("binoculars");
            _sadTexture = contentManager.Load<Texture2D>("sad");
            _drumstickTexture = contentManager.Load<Texture2D>("drumstick");


            if (_borderTexture == null)
            {
                _borderTexture = new Texture2D(GameWorld.Instance.GraphicsDevice, 1, 1);
                _borderTexture.SetData(new[] { Color.White });
            }
        }

        private void Update(GameTime gameTime)
        {
            UpdateHunger(gameTime);
            UpdateMood(gameTime);
            UpdateActivity(gameTime);
            UpdateSadThoughtBubbleDisplay(gameTime);

            if (!_isExiting)
            {
                TryRandomWalk(gameTime);
            }

            UpdatePathFollowing(gameTime);
            HandlePathCompletion();
        }

        private void UpdateHunger(GameTime gameTime)
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
        }

        private void UpdateMood(GameTime gameTime)
        {
            if (Hunger > HIGH_HUNGER_THRESHOLD)
            {
                _uncommittedMoodChangePoints -= MOOD_PENALTY_PER_SECOND_HIGH_HUNGER * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (Mood < 100)
            {
                _uncommittedMoodChangePoints += MOOD_RECOVERY_PER_SECOND_NOT_HUNGRY * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            if (Math.Abs(_uncommittedMoodChangePoints) >= 1.0f)
            {
                int wholePointsToChange = (int)_uncommittedMoodChangePoints;
                Mood += wholePointsToChange;
                _uncommittedMoodChangePoints -= wholePointsToChange;

                if (Mood < 0) Mood = 0;
                if (Mood > 100) Mood = 100;

                if (wholePointsToChange < 0)
                {
                    Debug.WriteLine($"Visitor {VisitorId} mood decreased by {Math.Abs(wholePointsToChange)} to {Mood} due to high hunger ({Hunger}).");
                }
                else if (wholePointsToChange > 0)
                {
                    Debug.WriteLine($"Visitor {VisitorId} mood increased by {wholePointsToChange} to {Mood} due to normal hunger ({Hunger}).");
                }
            }
        }

        private void UpdateActivity(GameTime gameTime)
        {
            if (!_isExiting)
            {
                if (currentHabitat != null)
                {
                    if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
                    {
                        currentVisitTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                        if (currentVisitTime >= VISIT_DURATION)
                        {
                            Habitat justLeftHabitat = currentHabitat;
                            if (justLeftHabitat != null)
                            {
                                if (DoesHabitatTriggerSadReaction(justLeftHabitat))
                                {
                                    _showSadThoughtBubbleTimer = SAD_BUBBLE_DURATION;
                                    int moodDeduction = (int)(Mood * 0.20f);
                                    Mood -= moodDeduction;
                                    if (Mood < 0) Mood = 0;
                                    Debug.WriteLine($"Visitor {VisitorId} mood decreased by {moodDeduction} to {Mood} after seeing sad animals.");
                                }

                                _visitedHabitatIds.Add(justLeftHabitat.HabitatId);
                                justLeftHabitat.LeaveHabitat(this);
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
                else if (currentShop != null)
                {
                    if (path == null || path.Count == 0 || currentNodeIndex >= path.Count)
                    {
                        currentVisitTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                        if (currentVisitTime >= VISIT_DURATION / 2)
                        {
                            Shop justLeftShop = currentShop;
                            if (justLeftShop != null)
                            {
                                justLeftShop.VisitorInteraction(this);
                                justLeftShop.LeaveShop(this);
                                currentShop = null;
                                currentVisitTime = 0f;
                                if (!_isExiting)
                                {
                                    PerformNextActionDecision();
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool DoesHabitatTriggerSadReaction(Habitat visitedHabitat)
        {
            if (visitedHabitat == null) return false;

            var animalsInHabitat = visitedHabitat.GetAnimals();
            if (animalsInHabitat != null)
            {
                foreach (var animal in animalsInHabitat)
                {
                    if (animal.Mood <= 50)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateSadThoughtBubbleDisplay(GameTime gameTime)
        {
            if (_showSadThoughtBubbleTimer > 0)
            {
                _showSadThoughtBubbleTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_showSadThoughtBubbleTimer < 0)
                {
                    _showSadThoughtBubbleTimer = 0;
                }
            }
        }

        private void UpdatePathFollowing(GameTime gameTime)
        {
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
        }

        private void HandlePathCompletion()
        {
            if (_isExiting && (path == null || path.Count == 0 || currentNodeIndex >= path.Count))
            {
                _isRunning = false;
                UpdateScoreBasedOnMood();
                GameWorld.Instance.ConfirmDespawn(this);
                return;
            }

            if (path != null && currentNodeIndex >= path.Count)
            {
                path = null;
                currentNodeIndex = 0;

                if (_isExiting)
                {
                    _isRunning = false;
                    UpdateScoreBasedOnMood();
                    GameWorld.Instance.ConfirmDespawn(this);
                }
            }
        }

        private void UpdateScoreBasedOnMood()
        {
            if(Mood > 50)
            {
                ScoreManager.Instance.Score += MOOD_INFLUENCE_ON_SCORE;
            }
            else if(Mood < 50)
            {
                ScoreManager.Instance.Score -= MOOD_INFLUENCE_ON_SCORE;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (sprite == null) return;
            lock (_positionLock)
            {
                spriteBatch.Draw(sprite, position, new Rectangle(0, 0, 32, 32), Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);

                if (_showSadThoughtBubbleTimer > 0 && _thoughtBubble != null && _sadTexture != null)
                {
                    _thoughtBubble.Draw(spriteBatch, position, sprite.Height, _sadTexture, new Rectangle(0, 0, _sadTexture.Width, _sadTexture.Height), 0.3f);
                }
                else if (Hunger > HUNGER_PRIORITY_THRESHOLD && _thoughtBubble != null && _drumstickTexture != null)
                {
                    _thoughtBubble.Draw(spriteBatch, position, sprite.Height, _drumstickTexture, null, 0.3f);
                }
                else
                {
                    DrawVisitingHabitatThoughtBubble(spriteBatch);
                }

                if (IsSelected)
                {
                    DrawBorder(spriteBatch, BoundingBox, 2, Color.Yellow); 
                }
            }
        }

        private void DrawVisitingHabitatThoughtBubble(SpriteBatch spriteBatch)
        {
            if (currentHabitat != null && 
                (path == null || path.Count == 0 || currentNodeIndex >= path.Count) && 
                _thoughtBubble != null && 
                _animalInThoughtTexture != null)
            {
                _thoughtBubble.Draw(spriteBatch, position, sprite.Height, _animalInThoughtTexture, contentScale: 0.35f);
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
            ExecuteSaveCommand(transaction);
        }

        private void ExecuteSaveCommand(SqliteTransaction transaction)
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

            Position = GameWorld.TileToPixel(new Vector2(posX, posY));

            InitializeVisitorState();
        }

        private void InitializeVisitorState()
        {
            pathfinder = new AStarPathfinding(GameWorld.GRID_WIDTH, GameWorld.GRID_HEIGHT, GameWorld.Instance.WalkableMap);
            path = null;
            currentNodeIndex = 0;
            timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;
            currentVisitTime = 0f;
            _visitedHabitatIds = new HashSet<int>();
            _isExiting = false;
            _uncommittedHungerPoints = 0f;
            _uncommittedMoodChangePoints = 0f;
        }

        public void InitiateExit()
        {
            Vector2 exitTileCoord = GameWorld.Instance.VisitorExitTileCoordinate; 
            
            path = null;
            currentNodeIndex = 0;
            if (currentHabitat != null)
            {
                currentHabitat.LeaveHabitat(this);
                currentHabitat = null;
            }
            if (currentShop != null)
            {
                currentShop.LeaveShop(this);
                currentShop = null;
            }
            currentVisitTime = 0f;

            if (TryPathfindToExit(exitTileCoord))
            {
                Debug.WriteLine($"Visitor {VisitorId}: Successfully pathfinding to exit. Path length: {path.Count}");
                _isExiting = true;
                timeSinceLastRandomWalk = float.MinValue;
            }
            else
            {
                Debug.WriteLine($"Visitor {VisitorId}: Failed to find path to exit. Resorting to random walk behavior.");
                _isExiting = false;
                timeSinceLastRandomWalk = RANDOM_WALK_INTERVAL;
            }
        }

        private bool TryPathfindToExit(Vector2 targetExitTile)
        {
            PathfindTo(targetExitTile);
            return path != null && path.Count > 0;
        }
    }
}
