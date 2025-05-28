using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        // Tile and grid settings
        public const int TILE_SIZE = 32; // Size of each tile in pixels
        public const int GRID_WIDTH = 100;
        public const int GRID_HEIGHT = 100;

        private static GameWorld _instance;
        private static readonly object _lock = new object();
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;  // Add font field
        Map map;
        TileRenderer tileRenderer;
        Texture2D[] tileTextures;
        private FPSCounter _fpsCounter;  // Add FPS counter field

        // UI
        Button shopButton;
        Texture2D shopIconTexture;
        private ShopWindow _shopWindow;

        // Money Management
        private MoneyDisplay _moneyDisplay;

        // Walkable map for pathfinding
        public bool[,] WalkableMap { get; private set; }

        // Fence and enclosure management
        private bool isPlacingEnclosure = true;
        private List<Habitat> habitats;
        private List<Visitor> visitors; // Add visitors list
        private int _nextHabitatId = 1;
        private int _nextAnimalId = 1;
        private int _nextVisitorId = 1;

        // Visitor spawning settings
        private float _visitorSpawnTimer = 0f;
        private const float VISITOR_SPAWN_INTERVAL = 10.0f; // Spawn every 10 seconds
        private Vector2 _visitorSpawnPosition;
        private const int VISITOR_SPAWN_REWARD = 20;

        // Public property to access the spawn/exit position
        public Vector2 VisitorSpawnExitPosition => _visitorSpawnPosition;

        // Camera instance
        private Camera _camera;

        // Window state
        private bool _isFullscreen = false;

        private List<Visitor> _visitorsToDespawn = new List<Visitor>(); // Added for despawning

        public List<Habitat> GetHabitats()
        {
            return habitats;
        }

        public List<Visitor> GetVisitors()
        {
            return visitors;
        }

        public static GameWorld Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameWorld();
                        }
                    }
                }
                return _instance;
            }
        }

        private GameWorld()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.IsFullScreen = false;
            Window.AllowUserResizing = true;
            _graphics.ApplyChanges();
            
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set to use monitor refresh rate instead of fixed 60 FPS
            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromTicks(1);

            // Initialize map first
            map = new Map(GRID_WIDTH, GRID_HEIGHT); 

            // Initialize camera
            _camera = new Camera(_graphics);
            _camera.SetMapDimensions(GRID_WIDTH * TILE_SIZE, GRID_HEIGHT * TILE_SIZE);

            // Initialize walkable map from the map object
            WalkableMap = map.ToWalkableArray();

            habitats = new List<Habitat>();
            visitors = new List<Visitor>(); // Initialize visitors list

            // Find the top-most path tile for visitor spawning
            Vector2 pathSpawnTile = Vector2.Zero; // Default to top-left if no path found
            bool foundSpawn = false;
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    // TextureIndex 1 is the path tile (Dirt1)
                    if (map.Tiles[x, y].TextureIndex == 1) 
                    {
                        pathSpawnTile = new Vector2(x, y);
                        foundSpawn = true;
                        break; 
                    }
                }
                if (foundSpawn) break;
            }
            _visitorSpawnPosition = TileToPixel(pathSpawnTile);

            // Initialize MoneyManager and MoneyDisplay
            MoneyManager.Instance.Initialize(0); // Initialize with 0, actual value loaded in Initialize()

            // Subscribe to window resize event
            Window.ClientSizeChanged += OnClientSizeChanged;
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (!_isFullscreen)
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();
                _camera.UpdateViewport(_graphics.GraphicsDevice.Viewport);
            }
        }

        // Convert pixel position to tile position
        public static Vector2 PixelToTile(Vector2 pixelPos)
        {
            return new Vector2(
                (int)(pixelPos.X / TILE_SIZE),
                (int)(pixelPos.Y / TILE_SIZE)
            );
        }

        // Convert tile position to pixel position (center of tile)
        public static Vector2 TileToPixel(Vector2 tilePos)
        {
            return new Vector2(
                tilePos.X * TILE_SIZE + TILE_SIZE / 2,
                tilePos.Y * TILE_SIZE + TILE_SIZE / 2
            );
        }

        protected override void Initialize()
        {
            var (loadedHabitats, nextHabitatId, nextAnimalId, nextVisitorId, loadedMoney) = DatabaseManager.Instance.LoadGame(Content);
            habitats = loadedHabitats;
            _nextHabitatId = nextHabitatId;
            _nextAnimalId = nextAnimalId;
            _nextVisitorId = nextVisitorId;
            MoneyManager.Instance.Initialize(loadedMoney);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");  // Load the font
            _fpsCounter = new FPSCounter(_font, _graphics);  // Initialize FPS counter with graphics manager

            Texture2D backgroundTexture = Content.Load<Texture2D>("Button_Blue"); // Brug det rigtige navn
            Texture2D iconTexture = Content.Load<Texture2D>("Regular_07");       // Brug det rigtige navn

            Vector2 shopButtonPosition = new Vector2(GraphicsDevice.Viewport.Width - backgroundTexture.Width - 10, 30);
            shopButton = new Button(backgroundTexture, iconTexture, shopButtonPosition);

            Texture2D shopBackgroundTexture = Content.Load<Texture2D>("Button_Blue_9Slides");
            Texture2D buttonTexture = Content.Load<Texture2D>("Button_Blue_3Slides");
            SpriteFont font = Content.Load<SpriteFont>("font");

            // Lav shop window
            Vector2 shopWindowPosition = new Vector2(1050, 90); // fx midt på skærmen
            _shopWindow = new ShopWindow(shopBackgroundTexture, buttonTexture, font, shopWindowPosition);


            // Initialize MoneyDisplay here after _font is loaded
            Vector2 moneyPosition = new Vector2(10, 10); // Top-left corner
            _moneyDisplay = new MoneyDisplay(_font, moneyPosition, Color.Black, 2f);
            MoneyManager.Instance.Attach(_moneyDisplay); // Attach MoneyDisplay as observer
            MoneyManager.Instance.Notify(); // Initial notification to set initial money text

            tileTextures = new Texture2D[2];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");

            // map = new Map(GRID_WIDTH, GRID_HEIGHT); // yo, this is where the size happens -- This line is now redundant
            tileRenderer = new TileRenderer(tileTextures);

            // Load fence textures
            FenceRenderer.LoadContent(Content);

            // Load content for all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.LoadAnimalContent(Content);
            }
            Habitat.LoadContent(Content);
        }

        MouseState prevMouseState;
        KeyboardState prevKeyboardState;
        

        private void PlaceFence(Vector2 pixelPosition)
        {
            Debug.WriteLine($"PlaceFence called with pixel position: {pixelPosition}, isPlacingEnclosure: {isPlacingEnclosure}");

            // Create and execute the place habitat command
            var placeHabitatCommand = new PlaceHabitatCommand(pixelPosition);
            CommandManager.Instance.ExecuteCommand(placeHabitatCommand);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _fpsCounter.Update(gameTime);  // Update FPS counter

            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            // Handle F11 for fullscreen toggle
            if (keyboard.IsKeyDown(Keys.F11) && !prevKeyboardState.IsKeyDown(Keys.F11))
            {
                ToggleFullscreen();
            }

            // Update camera
            _camera.Update(gameTime, mouse, prevMouseState, keyboard, prevKeyboardState);

            // Handle 'C' key press for toggling camera clamping
            if (keyboard.IsKeyDown(Keys.C) && !prevKeyboardState.IsKeyDown(Keys.C))
            {
                _camera.ToggleClamping();
            }

            // Convert mouse position to world coordinates
            Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

            // Handle 'A' key press for spawning animals
            if (keyboard.IsKeyDown(Keys.A) && !prevKeyboardState.IsKeyDown(Keys.A))
            {
                // Create and execute the place animal command
                var placeAnimalCommand = new PlaceAnimalCommand(worldMousePosition);
                CommandManager.Instance.ExecuteCommand(placeAnimalCommand);
            }

            // Handle automatic visitor spawning
            bool animalsExist = habitats.Any(h => h.GetAnimals().Count > 0);
            if (animalsExist)
            {
                _visitorSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_visitorSpawnTimer >= VISITOR_SPAWN_INTERVAL)
                {
                    _visitorSpawnTimer = 0f; // Reset timer

                    // Spawn visitor
                    Visitor newVisitor = new Visitor(_visitorSpawnPosition, _nextVisitorId++);
                    newVisitor.LoadContent(Content);
                    visitors.Add(newVisitor);

                    // Add money
                    MoneyManager.Instance.AddMoney(VISITOR_SPAWN_REWARD);
                    Debug.WriteLine($"Visitor spawned at {_visitorSpawnPosition}. Added ${VISITOR_SPAWN_REWARD}.");
                }
            }
            else
            {
                _visitorSpawnTimer = 0f; // Reset timer if no animals exist to prevent instant spawn when an animal is added
            }

            // Handle 'B' key press for manually spawning visitors (debugging)
            if (keyboard.IsKeyDown(Keys.B) && !prevKeyboardState.IsKeyDown(Keys.B))
            {
                Visitor newVisitor = new Visitor(_visitorSpawnPosition, _nextVisitorId++);
                newVisitor.LoadContent(Content);
                visitors.Add(newVisitor);
                Debug.WriteLine($"Manually spawned visitor at {_visitorSpawnPosition} for debugging.");
            }

            if (keyboard.IsKeyDown(Keys.S) && !prevKeyboardState.IsKeyDown(Keys.S))
            {
                DatabaseManager.Instance.SaveGame(habitats);
            }

            // Handle 'O' key press for clearing everything
            if (keyboard.IsKeyDown(Keys.O) && !prevKeyboardState.IsKeyDown(Keys.O))
            {
                habitats.Clear();
                visitors.Clear();
                _nextHabitatId = 1;
                _nextAnimalId = 1;
                _nextVisitorId = 1;
                CommandManager.Instance.Clear(); // Clear command history when clearing everything
            }

            // Handle 'M' key press for adding money (debugging)
            if (keyboard.IsKeyDown(Keys.M) && !prevKeyboardState.IsKeyDown(Keys.M))
            {
                MoneyManager.Instance.AddMoney(100000);
                Debug.WriteLine("Added $100,000 for debugging.");
            }

            // Handle Ctrl+Z for undo
            if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.Z) && 
                !prevKeyboardState.IsKeyDown(Keys.Z))
            {
                CommandManager.Instance.Undo();
            }

            // Handle Ctrl+Y for redo
            if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.Y) && 
                !prevKeyboardState.IsKeyDown(Keys.Y))
            {
                CommandManager.Instance.Redo();
            }

            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
            {
                // Make the first animal in the first habitat pathfind to the world mouse position
                if (habitats.Count > 0 && habitats[0].GetAnimals().Count > 0)
                {
                    habitats[0].GetAnimals()[0].PathfindTo(worldMousePosition);
                }
            }

            // Handle right mouse button for fence placement
            if (mouse.RightButton == ButtonState.Pressed && prevMouseState.RightButton != ButtonState.Pressed)
            {
                PlaceFence(worldMousePosition);
            }

            // Update all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.Update(gameTime);
            }

            // Process despawning visitors
            if (_visitorsToDespawn.Count > 0)
            {
                foreach (var visitorToRemove in _visitorsToDespawn)
                {
                    visitors.Remove(visitorToRemove);
                    Debug.WriteLine($"Visitor {visitorToRemove.VisitorId} has been despawned.");
                }
                _visitorsToDespawn.Clear();
            }
            MouseState mouseState = Mouse.GetState();
            shopButton.Update(mouseState);

            // Når du klikker på shop-ikonet, viser vi vinduet
            if (shopButton.IsClicked)
            {
                _shopWindow.IsVisible = !_shopWindow.IsVisible;
            }

            _shopWindow.Update(gameTime, mouseState);

            prevMouseState = mouse;
            prevKeyboardState = keyboard;

            base.Update(gameTime);
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            if (_isFullscreen)
            {
                _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                _graphics.IsFullScreen = true;
            }
            else
            {
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
                _graphics.IsFullScreen = false;
            }
            _graphics.ApplyChanges();
            _camera.UpdateViewport(_graphics.GraphicsDevice.Viewport);
        }

        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw game elements with camera transform
            Matrix transform = _camera.GetTransformMatrix();
            _spriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

            tileRenderer.Draw(_spriteBatch, map);

            // Draw all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.Draw(_spriteBatch);
            }

            // Draw all visitors
            foreach (var visitor in visitors)
            {
                visitor.Draw(_spriteBatch);
            }

            // VIGTIGT! Luk det første Begin!
            _spriteBatch.End();

            // Draw UI elements without camera offset
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw FPS counter
            _fpsCounter.Draw(_spriteBatch);

            // Draw instructions at the bottom of the screen
            string instructions = "Press right click for habitat\nPress 'A' for placing animal\nPress 'B' for spawning visitor\nPress 'S' to save\nPress 'O' to clear everything\nPress 'M' to add $100k (debug)\nPress 'F11' to toggle fullscreen\nUse middle mouse or arrow keys to move camera\nUse mouse wheel to zoom\nCtrl+Z to undo, Ctrl+Y to redo";
            Vector2 textPosition = new Vector2(10, _graphics.PreferredBackBufferHeight - 200);
            _spriteBatch.DrawString(_font, instructions, textPosition, Color.White);

            // Draw current money from MoneyDisplay
            _moneyDisplay.Draw(_spriteBatch);

            // Draw undo/redo status
            Vector2 undoRedoPosition = new Vector2(10, 40);
            string undoRedoText = $"Undo: {CommandManager.Instance.GetUndoDescription()}\nRedo: {CommandManager.Instance.GetRedoDescription()}";
            _spriteBatch.DrawString(_font, undoRedoText, undoRedoPosition, Color.LightBlue);

            // Tegn shop knappen
            shopButton.Draw(_spriteBatch);
            shopButton.Draw(_spriteBatch);
            _shopWindow.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public int GetNextAnimalId()
        {
            return _nextAnimalId++;
        }

        public int GetNextHabitatId()
        {
            return _nextHabitatId++;
        }

        public bool GetOriginalWalkableState(int x, int y)
        {
            return map.IsWalkable(x, y);
        }

        public List<Vector2> GetWalkableTileCoordinates()
        {
            var walkableTiles = new List<Vector2>();
            if (WalkableMap == null)
            {
                Debug.WriteLine("Warning: WalkableMap is null in GameWorld.GetWalkableTileCoordinates.");
                return walkableTiles; // Return empty list if map not initialized
            }

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (WalkableMap[x, y])
                    {
                        walkableTiles.Add(new Vector2(x, y));
                    }
                }
            }
            return walkableTiles;
        }

        public void ConfirmDespawn(Visitor visitor)
        {
            if (visitor != null && !_visitorsToDespawn.Contains(visitor) && !visitors.Contains(visitor)) // Ensure not already added and not already removed from main list
            {
                // Visitor should have already stopped its own update loop.
                _visitorsToDespawn.Add(visitor);
                Debug.WriteLine($"Visitor {visitor.VisitorId} confirmed exit and added to despawn queue.");
            }
            else if (visitor != null && visitors.Contains(visitor) && !_visitorsToDespawn.Contains(visitor)) // Standard case: visitor exists in main list and not yet in despawn queue
            {
                 _visitorsToDespawn.Add(visitor);
                Debug.WriteLine($"Visitor {visitor.VisitorId} confirmed exit and added to despawn queue.");
            }
            else if (visitor != null && _visitorsToDespawn.Contains(visitor))
            {
                Debug.WriteLine($"Visitor {visitor.VisitorId} already in despawn queue. Confirmation ignored.");
            }
            else
            {
                Debug.WriteLine($"Attempted to confirm despawn for a null or already processed/removed visitor.");
            }
        }
    }
}
