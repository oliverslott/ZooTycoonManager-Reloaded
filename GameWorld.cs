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

        // Walkable map for pathfinding
        public bool[,] WalkableMap { get; private set; }

        // Fence and enclosure management
        private bool isPlacingEnclosure = true;
        private List<Habitat> habitats;
        private List<Visitor> visitors; // Add visitors list

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
            // Create initial animal
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");  // Load the font
            tileTextures = new Texture2D[2];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");

            map = new Map(400, 400); // yo, this is where the size happens
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
            Habitat newHabitat = new Habitat(pixelPosition, Habitat.DEFAULT_ENCLOSURE_SIZE, Habitat.DEFAULT_ENCLOSURE_SIZE);
            habitats.Add(newHabitat);
            newHabitat.PlaceEnclosure(pixelPosition);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            // Handle 'A' key press for spawning animals
            if (keyboard.IsKeyDown(Keys.A) && !prevKeyboardState.IsKeyDown(Keys.A))
            {
                Vector2 mousePos = new Vector2(mouse.X, mouse.Y);
                // Find the habitat that contains the mouse position
                Habitat targetHabitat = habitats.FirstOrDefault(h => h.ContainsPosition(mousePos));
                if (targetHabitat != null)
                {
                    targetHabitat.SpawnAnimal(mousePos);
                }
            }

            // Handle 'B' key press for spawning visitors
            if (keyboard.IsKeyDown(Keys.B) && !prevKeyboardState.IsKeyDown(Keys.B))
            {
                Vector2 mousePos = new Vector2(mouse.X, mouse.Y);
                Vector2 tilePos = PixelToTile(mousePos);
                Vector2 spawnPos = TileToPixel(tilePos);
                Visitor newVisitor = new Visitor(spawnPos);
                newVisitor.LoadContent(Content);
                visitors.Add(newVisitor);
            }

            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
            {
                Vector2 clickPosition = new Vector2(mouse.X, mouse.Y);
                // Make the first animal in the first habitat pathfind to the clicked position
                if (habitats.Count > 0 && habitats[0].GetAnimals().Count > 0)
                {
                    habitats[0].GetAnimals()[0].PathfindTo(clickPosition);
                }
            }

            // Handle right mouse button for fence placement
            if (mouse.RightButton == ButtonState.Pressed && prevMouseState.RightButton != ButtonState.Pressed)
            {
                Vector2 clickPosition = new Vector2(mouse.X, mouse.Y);
                PlaceFence(clickPosition);
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

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

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

            // Draw instructions at the bottom of the screen
            string instructions = "Press right click for habitat\nPress 'A' for placing animal\nPress 'B' for spawning visitor";
            Vector2 textPosition = new Vector2(10, GRID_HEIGHT * TILE_SIZE - 70);
            _spriteBatch.DrawString(_font, instructions, textPosition, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
