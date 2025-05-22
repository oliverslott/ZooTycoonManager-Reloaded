using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        // Tile and grid settings
        public const int TILE_SIZE = 32; // Size of each tile in pixels
        public const int GRID_WIDTH = 40; // 1280 / 32
        public const int GRID_HEIGHT = 22; // 720 / 32

        private static GameWorld _instance;
        private static readonly object _lock = new object();
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;  // Add font field
        Map map;
        TileRenderer tileRenderer;
        Texture2D[] tileTextures;
        private FPSCounter _fpsCounter;  // Add FPS counter field

        // Walkable map for pathfinding
        public bool[,] WalkableMap { get; private set; }

        // Fence and enclosure management
        private bool isPlacingEnclosure = true;
        private List<Habitat> habitats;
        private List<Visitor> visitors; // Add visitors list
        private int _nextHabitatId = 1;
        private int _nextAnimalId = 1;
        private int _nextVisitorId = 1;

        // Camera instance
        private Camera _camera;

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
            _graphics.PreferredBackBufferWidth = GRID_WIDTH * TILE_SIZE;
            _graphics.PreferredBackBufferHeight = GRID_HEIGHT * TILE_SIZE;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set to use monitor refresh rate instead of fixed 60 FPS
            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromTicks(1);

            // Initialize camera
            _camera = new Camera(_graphics);

            // Initialize walkable map
            WalkableMap = new bool[GRID_WIDTH, GRID_HEIGHT];
            for (int x = 0; x < GRID_WIDTH; x++)
                for (int y = 0; y < GRID_HEIGHT; y++)
                    WalkableMap[x, y] = true;

            habitats = new List<Habitat>();
            visitors = new List<Visitor>(); // Initialize visitors list
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
            var (loadedHabitats, nextHabitatId, nextAnimalId, nextVisitorId) = DatabaseManager.Instance.LoadGame(Content);
            habitats = loadedHabitats;
            _nextHabitatId = nextHabitatId;
            _nextAnimalId = nextAnimalId;
            _nextVisitorId = nextVisitorId;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");  // Load the font
            _fpsCounter = new FPSCounter(_font);  // Initialize FPS counter
            tileTextures = new Texture2D[2];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");

            map = new Map(100, 100); // yo, this is where the size happens
            tileRenderer = new TileRenderer(tileTextures);

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
            
            // Create a new habitat and place its enclosure
            Habitat newHabitat = new Habitat(pixelPosition, Habitat.DEFAULT_ENCLOSURE_SIZE, Habitat.DEFAULT_ENCLOSURE_SIZE, _nextHabitatId++);
            habitats.Add(newHabitat);
            newHabitat.PlaceEnclosure(pixelPosition);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _fpsCounter.Update(gameTime);  // Update FPS counter

            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            // Update camera
            _camera.Update(gameTime, mouse, prevMouseState, keyboard, prevKeyboardState);

            // Convert mouse position to world coordinates
            Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

            // Handle 'A' key press for spawning animals
            if (keyboard.IsKeyDown(Keys.A) && !prevKeyboardState.IsKeyDown(Keys.A))
            {
                // Find the habitat that contains the world mouse position
                Habitat targetHabitat = habitats.FirstOrDefault(h => h.ContainsPosition(worldMousePosition));
                if (targetHabitat != null)
                {
                    targetHabitat.SpawnAnimal(worldMousePosition);
                }
            }

            // Handle 'B' key press for spawning visitors
            if (keyboard.IsKeyDown(Keys.B) && !prevKeyboardState.IsKeyDown(Keys.B))
            {
                Vector2 tilePos = PixelToTile(worldMousePosition);
                Vector2 spawnPos = TileToPixel(tilePos);
                Visitor newVisitor = new Visitor(spawnPos, _nextVisitorId++);
                newVisitor.LoadContent(Content);
                visitors.Add(newVisitor);
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

            prevMouseState = mouse;
            prevKeyboardState = keyboard;

            base.Update(gameTime);
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

            _spriteBatch.End();

            // Draw UI elements without camera offset
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw FPS counter
            _fpsCounter.Draw(_spriteBatch);

            // Draw instructions at the bottom of the screen
            string instructions = "Press right click for habitat\nPress 'A' for placing animal\nPress 'B' for spawning visitor\nPress 'S' to save\nPress 'O' to clear everything\nUse middle mouse or arrow keys to move camera\nUse mouse wheel to zoom";
            Vector2 textPosition = new Vector2(10, GRID_HEIGHT * TILE_SIZE - 130);
            _spriteBatch.DrawString(_font, instructions, textPosition, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public int GetNextAnimalId()
        {
            return _nextAnimalId++;
        }
    }
}
