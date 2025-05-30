using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZooTycoonManager.Commands;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        public const int TILE_SIZE = 32;
        public const int GRID_WIDTH = 100;
        public const int GRID_HEIGHT = 100;

        public const int VISITOR_SPAWN_TILE_X = 20;
        public const int VISITOR_SPAWN_TILE_Y = 0;
        public const int ROAD_TEXTURE_INDEX = 1;

        public const int VISITOR_EXIT_TILE_X = GRID_WIDTH - 20;
        public const int VISITOR_EXIT_TILE_Y = 0;

        private static GameWorld _instance;
        private static readonly object _lock = new object();
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        public Map map { get; private set; }
        TileRenderer tileRenderer;
        private Texture2D[] tileTextures;
        private FPSCounter _fpsCounter; 
        private Texture2D _habitatPreviewTexture;

        // UI
        Button shopButton;
        Texture2D shopIconTexture;
        private ShopWindow _shopWindow;
        SubMenuWindow _buildingsMenu;
        SubMenuWindow _habitatMenu;
        SubMenuWindow _animalMenu;
        SubMenuWindow _zookeeperMenu;

        private Texture2D _treeTexture;
        private List<Vector2> _staticTreePositions;
        private const int NUMBER_OF_STATIC_TREES = 1000;

        private List<Vector2> _boundaryFenceTilePositions;
        private HashSet<Vector2> _boundaryFenceTileCoordinates;

        private MoneyDisplay _moneyDisplay;

        public bool[,] WalkableMap { get; private set; }

        private enum PlacementMode
        {
            None,
            PlaceMediumHabitat,
            PlaceAnimal_Buffalo
        }

        private PlacementMode _currentPlacement = PlacementMode.None;

        private bool isPlacingEnclosure = true;
        private List<Habitat> habitats;
        private List<Visitor> visitors;
        private int _nextHabitatId = 1;
        private int _nextAnimalId = 1;
        private int _nextVisitorId = 1;

        private float _visitorSpawnTimer = 0f;
        private const float VISITOR_SPAWN_INTERVAL = 10.0f;
        private Vector2 _visitorSpawnTileCoord;
        private Vector2 _visitorExitTileCoord;
        private const int VISITOR_SPAWN_REWARD = 20;
        public Vector2 VisitorSpawnTileCoordinate => _visitorSpawnTileCoord;
        public Vector2 VisitorExitTileCoordinate => _visitorExitTileCoord;

        private Camera _camera;

        private bool _isFullscreen = false;

        private bool _isPlacingRoadModeActive = false;

        private List<Visitor> _visitorsToDespawn = new List<Visitor>();

        private EntityInfoPopup _entityInfoPopup;
        private IInspectableEntity _selectedEntity;

        private bool IsMouseOverUI(Vector2 mousePosition)
        {
            Rectangle mouseRect = new Rectangle((int)mousePosition.X, (int)mousePosition.Y, 1, 1);

            // Vi antager at UI sidder fast på skærmen, så vi tjekker mod deres skærmpositioner
            if (_shopWindow.IsVisible && _shopWindow.Contains(mousePosition)) return true;
            if (_buildingsMenu.IsVisible && _buildingsMenu.Contains(mousePosition)) return true;
            if (_habitatMenu.IsVisible && _habitatMenu.Contains(mousePosition)) return true;
            if (_animalMenu.IsVisible && _animalMenu.Contains(mousePosition)) return true;
            if (_zookeeperMenu.IsVisible && _zookeeperMenu.Contains(mousePosition)) return true;
            if (shopButton.Contains(mousePosition)) return true;

            return false;
        }

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

            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromTicks(1);

            map = new Map(GRID_WIDTH, GRID_HEIGHT);

            _camera = new Camera(_graphics);
            _camera.SetMapDimensions(GRID_WIDTH * TILE_SIZE, GRID_HEIGHT * TILE_SIZE);

            WalkableMap = map.ToWalkableArray();

            habitats = new List<Habitat>();
            visitors = new List<Visitor>();
            _boundaryFenceTilePositions = new List<Vector2>();
            _boundaryFenceTileCoordinates = new HashSet<Vector2>();

            _visitorSpawnTileCoord = new Vector2(VISITOR_SPAWN_TILE_X, VISITOR_SPAWN_TILE_Y);
            _visitorExitTileCoord = new Vector2(VISITOR_EXIT_TILE_X, VISITOR_EXIT_TILE_Y);

            MoneyManager.Instance.Initialize(0);

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
                Vector2 newShopPos = new Vector2(
                    _graphics.PreferredBackBufferWidth - 260, // juster tallet hvis nødvendigt
                    90
);
                _shopWindow.Reposition(newShopPos);
                Vector2 newShopButtonPos = new Vector2(
                    _graphics.PreferredBackBufferWidth - shopButton.GetWidth() - 10,
                    30
);
                shopButton.SetPosition(newShopButtonPos);
                Vector2 newSubMenuPos = new Vector2(
                _graphics.PreferredBackBufferWidth - 465, // justér hvis nødvendigt
                90
);

                _buildingsMenu.Reposition(newSubMenuPos);
                _habitatMenu.Reposition(newSubMenuPos);
                _animalMenu.Reposition(newSubMenuPos);
                _zookeeperMenu.Reposition(newSubMenuPos);
            }
        }


        public static Vector2 PixelToTile(Vector2 pixelPos)
        {
            return new Vector2(
                (int)Math.Floor(pixelPos.X / TILE_SIZE),
                (int)Math.Floor(pixelPos.Y / TILE_SIZE)
            );
        }


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
            _font = Content.Load<SpriteFont>("font"); 
            _fpsCounter = new FPSCounter(_font, _graphics);
            _habitatPreviewTexture = Content.Load<Texture2D>("fencesprite2");


            Texture2D backgroundTexture = Content.Load<Texture2D>("Button_Blue"); // Brug det rigtige navn
            Texture2D iconTexture = Content.Load<Texture2D>("Regular_07");       // Brug det rigtige navn

            Vector2 shopButtonPosition = new Vector2(GraphicsDevice.Viewport.Width - backgroundTexture.Width - 10, 30);
            shopButton = new Button(backgroundTexture, iconTexture, shopButtonPosition);

            Texture2D shopBackgroundTexture = Content.Load<Texture2D>("Button_Blue_9Slides");
            Texture2D buttonTexture = Content.Load<Texture2D>("Button_Blue_3Slides");
            SpriteFont font = Content.Load<SpriteFont>("font");

            string[] buildings = { "Tiles", "Visitor Shop", "Tree", "Waterhole" };
            string[] habitattype = { "Small", "Medium", "Large" };
            string[] animals = { "Buffalo", "Turtle", "Chimpanzee", "Camel", "Orangutang", "Kangaroo", "Wolf", "Bear", "Elephant", "Polarbear" };
            string[] zookeepers = { "Experienced Zookeeper" };

            Vector2 subMenuPos = new Vector2(870, 75); // eller placer det ift. shopButton

            _buildingsMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, _font, subMenuPos, buildings);
            _habitatMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, _font, subMenuPos, habitattype);
            _animalMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, _font, subMenuPos, animals);
            _zookeeperMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, _font, subMenuPos, zookeepers);

            // Lav shop window
            Vector2 shopWindowPosition = new Vector2(1070, 90); // fx midt på skærmen
            _shopWindow = new ShopWindow(shopBackgroundTexture, buttonTexture, font, shopWindowPosition);


            // Initialize MoneyDisplay here after _font is loaded
            Vector2 moneyPosition = new Vector2(10, 10); // Top-left corner
            _moneyDisplay = new MoneyDisplay(_font, moneyPosition, Color.Black, 2f);
            MoneyManager.Instance.Attach(_moneyDisplay); // Attach MoneyDisplay as observer
            MoneyManager.Instance.Notify(); // Initial notification to set initial money text

            // Initialize AnimalInfoPopup here after _font is loaded
            _entityInfoPopup = new EntityInfoPopup(GraphicsDevice, _font); // Changed from AnimalInfoPopup

            tileTextures = new Texture2D[2];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");

            tileRenderer = new TileRenderer(tileTextures, TILE_SIZE);

            FenceRenderer.LoadContent(Content);
            InitializeBoundaryFences(); 

            _treeTexture = Content.Load<Texture2D>("tree1");
            InitializeStaticTrees();

            foreach (var habitat in habitats)
            {
                habitat.LoadAnimalContent(Content);
            }
            Habitat.LoadContent(Content);
        }

        MouseState prevMouseState;
        KeyboardState prevKeyboardState;

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

            _camera.Update(gameTime, mouse, prevMouseState, keyboard, prevKeyboardState);

            if (keyboard.IsKeyDown(Keys.C) && !prevKeyboardState.IsKeyDown(Keys.C))
            {
                _camera.ToggleClamping();
            }

            if (keyboard.IsKeyDown(Keys.P) && !prevKeyboardState.IsKeyDown(Keys.P))
            {
                if (_isPlacingRoadModeActive)
                {
                    _isPlacingRoadModeActive = false;
                    Debug.WriteLine("Exited tile placement mode");
                }
            }


            Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));



            

            bool animalsExist = habitats.Any(h => h.GetAnimals().Count > 0);
            if (animalsExist)
            {
                _visitorSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_visitorSpawnTimer >= VISITOR_SPAWN_INTERVAL)
                {
                    _visitorSpawnTimer = 0f; 

                    Visitor newVisitor = new Visitor(_visitorSpawnTileCoord, _nextVisitorId++);
                    newVisitor.LoadContent(Content);
                    visitors.Add(newVisitor);

                    MoneyManager.Instance.AddMoney(VISITOR_SPAWN_REWARD);
                    Debug.WriteLine($"Visitor spawned at {_visitorSpawnTileCoord}. Added ${VISITOR_SPAWN_REWARD}.");
                }
            }
            else
            {
                _visitorSpawnTimer = 0f; // Reset timer if no animals exist to prevent instant spawn when an animal is added
            }

            if (keyboard.IsKeyDown(Keys.B) && !prevKeyboardState.IsKeyDown(Keys.B))
            {
                Visitor newVisitor = new Visitor(_visitorSpawnTileCoord, _nextVisitorId++);
                newVisitor.LoadContent(Content);
                visitors.Add(newVisitor);
                Debug.WriteLine($"Manually spawned visitor at {_visitorSpawnTileCoord} for debugging.");
            }

            if (_isPlacingRoadModeActive && mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
            {
                Vector2 screenMousePos = new Vector2(mouse.X, mouse.Y);
                if (!IsMouseOverUI(screenMousePos))
                {
                    PlaceRoadTile(PixelToTile(worldMousePosition));
                }
            }
            else if (!_isPlacingRoadModeActive && mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
            {
                if (habitats.Count > 0 && habitats[0].GetAnimals().Count > 0)
                {
                    habitats[0].GetAnimals()[0].PathfindTo(worldMousePosition);
                }
            }

            if (keyboard.IsKeyDown(Keys.S) && !prevKeyboardState.IsKeyDown(Keys.S))
            {
                DatabaseManager.Instance.SaveGame(habitats);
            }

            if (keyboard.IsKeyDown(Keys.O) && !prevKeyboardState.IsKeyDown(Keys.O))
            {
                habitats.Clear();
                visitors.Clear();
                _nextHabitatId = 1;
                _nextAnimalId = 1;
                _nextVisitorId = 1;
                CommandManager.Instance.Clear(); // Clear command history when clearing everything
            }

            if (keyboard.IsKeyDown(Keys.M) && !prevKeyboardState.IsKeyDown(Keys.M))
            {
                MoneyManager.Instance.AddMoney(100000);
                Debug.WriteLine("Added $100,000 for debugging.");
            }

            if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.Z) &&
                !prevKeyboardState.IsKeyDown(Keys.Z))
            {
                CommandManager.Instance.Undo();
            }

            if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.Y) &&
                !prevKeyboardState.IsKeyDown(Keys.Y))
            {
                CommandManager.Instance.Redo();
            }

            bool popupHandledClick = _entityInfoPopup.Update(mouse, prevMouseState);

            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed && !popupHandledClick)
            {
                Vector2 worldMousePos = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

                if (_currentPlacement == PlacementMode.PlaceMediumHabitat)
                {
                    var command = new PlaceHabitatCommand(worldMousePos, cost: 10000);
                    CommandManager.Instance.ExecuteCommand(command);
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Buffalo)
                {
                    var command = new PlaceAnimalCommand(worldMousePos);
                    CommandManager.Instance.ExecuteCommand(command);

                    

                    _currentPlacement = PlacementMode.None;
                }
                else
                {
                    // evt. eksisterende logik for andre klik
                    // Convert mouse position to world coordinates for checking entity clicks
                    Vector2 worldMousePosForEntityCheck = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
                    bool entityClickedThisFrame = false;
                    IInspectableEntity clickedEntity = null;

                    // Check for animal clicks first
                    foreach (var habitat in habitats)
                    {
                        foreach (var animal in habitat.GetAnimals())
                        {
                            if (animal.BoundingBox.Contains(worldMousePosForEntityCheck))
                            {
                                clickedEntity = animal;
                                entityClickedThisFrame = true;
                                break;
                            }
                        }
                        if (entityClickedThisFrame) break;
                    }

                    if (!entityClickedThisFrame)
                    {
                        foreach (var visitor in visitors)
                        {
                            Rectangle visitorBoundingBox = new Rectangle((int)(visitor.Position.X - 16), (int)(visitor.Position.Y - 16), 32, 32);
                            if (visitorBoundingBox.Contains(worldMousePosForEntityCheck))
                            {
                                clickedEntity = visitor;
                                entityClickedThisFrame = true;
                                break;
                            }
                        }
                    }

                    if (clickedEntity != null)
                    {
                        if (_selectedEntity != null && _selectedEntity != clickedEntity)
                        {
                            _selectedEntity.IsSelected = false; 
                        }
                        _selectedEntity = clickedEntity;
                        _selectedEntity.IsSelected = true;
                        _entityInfoPopup.Show(_selectedEntity);
                    }

                    else if (!entityClickedThisFrame && _entityInfoPopup.IsVisible && !popupHandledClick)
                    {
                        _entityInfoPopup.Hide();
                        if (_selectedEntity != null)
                        {
                            _selectedEntity.IsSelected = false;
                            _selectedEntity = null;
                        }
                    }
                }
            }            


            foreach (var habitat in habitats)
            {
                habitat.Update(gameTime);
            }

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
            shopButton.Update(mouseState, prevMouseState);

            // Når du klikker på shop-ikonet, viser vi vinduet
            if (shopButton.IsClicked)
            {
                // Toggle shop window
                _shopWindow.IsVisible = !_shopWindow.IsVisible;

                // Luk alle under-menuer, uanset hvad
                _buildingsMenu.IsVisible = false;
                _habitatMenu.IsVisible = false;
                _animalMenu.IsVisible = false;
                _zookeeperMenu.IsVisible = false;
            }

            _shopWindow.Update(gameTime, mouseState, prevMouseState);
            _buildingsMenu.Update(mouseState, prevMouseState);
            _habitatMenu.Update(mouseState, prevMouseState);
            _animalMenu.Update(mouseState, prevMouseState);
            _zookeeperMenu.Update(mouseState, prevMouseState);

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
            Vector2 newShopPos = new Vector2(
                    _graphics.PreferredBackBufferWidth - 260, // juster tallet hvis nødvendigt
                    90
);
            _shopWindow.Reposition(newShopPos);
            Vector2 newShopButtonPos = new Vector2(
                _graphics.PreferredBackBufferWidth - shopButton.GetWidth() - 10,
                30
);
            shopButton.SetPosition(newShopButtonPos);
            Vector2 newSubMenuPos = new Vector2(
                _graphics.PreferredBackBufferWidth - 465, // justér hvis nødvendigt
                90
);

            _buildingsMenu.Reposition(newSubMenuPos);
            _habitatMenu.Reposition(newSubMenuPos);
            _animalMenu.Reposition(newSubMenuPos);
            _zookeeperMenu.Reposition(newSubMenuPos);

        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw game elements with camera transform
            Matrix transform = _camera.GetTransformMatrix();
            _spriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

            tileRenderer.Draw(_spriteBatch, map);

            // Draw static trees first, so they are behind other elements like roads/habitats if they overlap visually
            // in the expanded view area.
            if (_treeTexture != null && _staticTreePositions != null)
            {
                foreach (var treePos in _staticTreePositions)
                {
                    _spriteBatch.Draw(_treeTexture, treePos, Color.White);
                }
            }

            // Draw boundary fences
            if (_boundaryFenceTilePositions != null && _boundaryFenceTilePositions.Count > 0)
            {
                FenceRenderer.Draw(_spriteBatch, _boundaryFenceTilePositions, _boundaryFenceTileCoordinates, 2.67f);
            }

            // Draw road preview if in road placement mode
            if (_isPlacingRoadModeActive)
            {
                MouseState mouse = Mouse.GetState();
                Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
                Vector2 tilePreviewPosition = PixelToTile(worldMousePosition);

                // Ensure preview is within bounds
                if (tilePreviewPosition.X >= 0 && tilePreviewPosition.X < GRID_WIDTH &&
                    tilePreviewPosition.Y >= 0 && tilePreviewPosition.Y < GRID_HEIGHT)
                {
                    Rectangle destinationRectangle = new Rectangle(
                        (int)tilePreviewPosition.X * TILE_SIZE,
                        (int)tilePreviewPosition.Y * TILE_SIZE,
                        TILE_SIZE,
                        TILE_SIZE
                    );
                    _spriteBatch.Draw(tileTextures[ROAD_TEXTURE_INDEX], destinationRectangle, Color.White * 0.5f); // 50% transparency
                }
            }

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
            if (_currentPlacement == PlacementMode.PlaceMediumHabitat)
            {
                Vector2 mousePos = Mouse.GetState().Position.ToVector2();
                Vector2 worldMouse = _camera.ScreenToWorld(mousePos);

                // Snap til nærmeste tile
                Vector2 snappedTile = new Vector2(
                    (int)(worldMouse.X / TILE_SIZE),
                    (int)(worldMouse.Y / TILE_SIZE)
                );

                Vector2 snappedPos = new Vector2(
                    snappedTile.X * TILE_SIZE,
                    snappedTile.Y * TILE_SIZE
                );

                int habitatSize = Habitat.DEFAULT_ENCLOSURE_SIZE; // fx 5
                int previewPixelSize = (int)(habitatSize * TILE_SIZE * 1.25f); // 5% større
                //int previewPixelSize = 165;

                // Justér til midten af habitat
                Vector2 previewTopLeft = snappedPos + new Vector2(TILE_SIZE / 2) - new Vector2(previewPixelSize / 2);

                Rectangle destRect = new Rectangle(
                    (int)previewTopLeft.X,
                    (int)previewTopLeft.Y,
                    previewPixelSize,
                    previewPixelSize
                );

                _spriteBatch.Draw(_habitatPreviewTexture, destRect, Color.White * 0.5f); // Halvgennemsigtig
            }

            // VIGTIGT! Luk det første Begin!
            _spriteBatch.End();

            //Begin ui:
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_isPlacingRoadModeActive)
            {
                Vector2 infoPosition = new Vector2(550, 650);
                _spriteBatch.DrawString(_font, "Press Mouse 1 to place tiles", infoPosition, Color.Yellow);
                _spriteBatch.DrawString(_font, "Press P to exit tile mode", infoPosition + new Vector2(0, 25), Color.Yellow);
            }



            _fpsCounter.Draw(_spriteBatch);

            string instructions = "Press 'A' for placing animal\nPress 'B' for spawning visitor\nPress 'P' to toggle road placement\nPress 'S' to save\nPress 'O' to clear everything\nPress 'M' to add $100k (debug)\nPress 'F11' to toggle fullscreen\nUse middle mouse or arrow keys to move camera\nUse mouse wheel to zoom\nCtrl+Z to undo, Ctrl+Y to redo";
            Vector2 textPosition = new Vector2(10, _graphics.PreferredBackBufferHeight - 200);
            _spriteBatch.DrawString(_font, instructions, textPosition, Color.White);

            _moneyDisplay.Draw(_spriteBatch);

            Vector2 undoRedoPosition = new Vector2(10, 40);
            string undoRedoText = $"Undo: {CommandManager.Instance.GetUndoDescription()}\nRedo: {CommandManager.Instance.GetRedoDescription()}";
            _spriteBatch.DrawString(_font, undoRedoText, undoRedoPosition, Color.LightBlue);

            // Tegn shop knappen
            shopButton.Draw(_spriteBatch);
            shopButton.Draw(_spriteBatch);
            _shopWindow.Draw(_spriteBatch);
            _buildingsMenu.Draw(_spriteBatch);
            _habitatMenu.Draw(_spriteBatch);
            _animalMenu.Draw(_spriteBatch);
            _zookeeperMenu.Draw(_spriteBatch);
            // Draw AnimalInfoPopup
            _entityInfoPopup.Draw(_spriteBatch);

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
                return walkableTiles;
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
            if (visitor != null && !_visitorsToDespawn.Contains(visitor) && !visitors.Contains(visitor)) 
            {
                _visitorsToDespawn.Add(visitor);
                Debug.WriteLine($"Visitor {visitor.VisitorId} confirmed exit and added to despawn queue.");
            }
            else if (visitor != null && visitors.Contains(visitor) && !_visitorsToDespawn.Contains(visitor)) 
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

        private void PlaceRoadTile(Vector2 tilePosition)
        {
            int x = (int)tilePosition.X;
            int y = (int)tilePosition.Y;

            if (x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT)
            {
                if (map.Tiles[x, y].Walkable && map.Tiles[x, y].TextureIndex == ROAD_TEXTURE_INDEX)
                {
                    return;
                }


                Tile originalTile = map.Tiles[x, y];

                var placeRoadCommand = new PlaceRoadCommand(tilePosition, originalTile);
                CommandManager.Instance.ExecuteCommand(placeRoadCommand);
            }
            else
            {
                Debug.WriteLine($"Attempted to place road tile out of bounds at ({x}, {y})");
            }
        }

        public void UpdateTile(int x, int y, bool walkable, int textureIndex)
        {
            if (x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT)
            {
                map.Tiles[x, y].Walkable = walkable;
                map.Tiles[x, y].TextureIndex = textureIndex;

                if (WalkableMap != null)
                {
                    WalkableMap[x, y] = walkable;
                }
            }
            else
            {
                Debug.WriteLine($"Attempted to update tile out of bounds at ({x}, {y})");
            }
        }

        private void InitializeStaticTrees()
        {
            _staticTreePositions = new List<Vector2>();
            Random random = new Random();

            float mapWidthPixels = GRID_WIDTH * TILE_SIZE;
            float mapHeightPixels = GRID_HEIGHT * TILE_SIZE;

            float minX_bounds = -Camera.CAMERA_BOUNDS_BUFFER;
            float maxX_bounds = mapWidthPixels + Camera.CAMERA_BOUNDS_BUFFER;
            float minY_bounds = -Camera.CAMERA_BOUNDS_BUFFER;
            float maxY_bounds = mapHeightPixels + Camera.CAMERA_BOUNDS_BUFFER;

            Rectangle gridArea = new Rectangle(0, 0, (int)mapWidthPixels, (int)mapHeightPixels);

            for (int i = 0; i < NUMBER_OF_STATIC_TREES; i++)
            {
                float x_center, y_center;
                bool positionFound = false;
                int attempts = 0;

                while (!positionFound && attempts < 200) 
                {
                    int borderChoice = random.Next(4); 

                    if (borderChoice == 0) // Top border
                    {
                        x_center = (float)(random.NextDouble() * (maxX_bounds - minX_bounds) + minX_bounds);
                        y_center = (float)(random.NextDouble() * (gridArea.Top - minY_bounds) + minY_bounds);
                    }
                    else if (borderChoice == 1) // Bottom border
                    {
                        x_center = (float)(random.NextDouble() * (maxX_bounds - minX_bounds) + minX_bounds);
                        y_center = (float)(gridArea.Bottom + random.NextDouble() * (maxY_bounds - gridArea.Bottom));
                    }
                    else if (borderChoice == 2) // Left border
                    {
                        x_center = (float)(random.NextDouble() * (gridArea.Left - minX_bounds) + minX_bounds);
                        y_center = (float)(random.NextDouble() * (maxY_bounds - minY_bounds) + minY_bounds);
                    }
                    else // Right border
                    {
                        x_center = (float)(gridArea.Right + random.NextDouble() * (maxX_bounds - gridArea.Right));
                        y_center = (float)(random.NextDouble() * (maxY_bounds - minY_bounds) + minY_bounds);
                    }

                    float treeHalfWidth = _treeTexture.Width / 2f;
                    float treeHalfHeight = _treeTexture.Height / 2f;

                    Vector2 topLeftDrawPosition = new Vector2(x_center - treeHalfWidth, y_center - treeHalfHeight);
                    Rectangle treeBounds = new Rectangle((int)topLeftDrawPosition.X, (int)topLeftDrawPosition.Y, _treeTexture.Width, _treeTexture.Height);

                    if (!treeBounds.Intersects(gridArea))
                    {
                        _staticTreePositions.Add(topLeftDrawPosition);
                        positionFound = true;
                    }
                    attempts++;
                }
                if (!positionFound)
                {
                    Debug.WriteLine("Could not find a suitable position for a static tree after several attempts.");
                }
            }
        }

        private void InitializeBoundaryFences()
        {
            _boundaryFenceTilePositions.Clear();
            _boundaryFenceTileCoordinates.Clear();

            for (int x = -1; x <= GRID_WIDTH; x++)
            {
                if (x == VISITOR_SPAWN_TILE_X && -1 == VISITOR_SPAWN_TILE_Y - 1)
                {
                    continue;
                }
                if (x == VISITOR_EXIT_TILE_X && -1 == VISITOR_EXIT_TILE_Y - 1)
                {
                    continue;
                }

                Vector2 tilePos = new Vector2(x, -1);
                _boundaryFenceTilePositions.Add(tilePos);
                _boundaryFenceTileCoordinates.Add(tilePos);
            }

            for (int x = -1; x <= GRID_WIDTH; x++)
            {
                Vector2 tilePos = new Vector2(x, GRID_HEIGHT);
                _boundaryFenceTilePositions.Add(tilePos);
                _boundaryFenceTileCoordinates.Add(tilePos);
            }

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                Vector2 tilePos = new Vector2(-1, y);
                _boundaryFenceTilePositions.Add(tilePos);
                _boundaryFenceTileCoordinates.Add(tilePos);
            }

            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                Vector2 tilePos = new Vector2(GRID_WIDTH, y);
                _boundaryFenceTilePositions.Add(tilePos);
                _boundaryFenceTileCoordinates.Add(tilePos);
            }
        }
        public void ShowSubMenu(string type)
        {
            // Hvis den allerede er åben → luk den
            if ((type == "Buildings" && _buildingsMenu.IsVisible) ||
                (type == "Habitats" && _habitatMenu.IsVisible) ||
                (type == "Animals" && _animalMenu.IsVisible) ||
                (type == "Zookeepers" && _zookeeperMenu.IsVisible))
            {
                _buildingsMenu.IsVisible = false;
                _habitatMenu.IsVisible = false;
                _animalMenu.IsVisible = false;
                _zookeeperMenu.IsVisible = false;
                return;
            }

            // Ellers → vis den ønskede og luk de andre
            _buildingsMenu.IsVisible = false;
            _habitatMenu.IsVisible = false;
            _animalMenu.IsVisible = false;
            _zookeeperMenu.IsVisible = false;

            switch (type)
            {
                case "Buildings": _buildingsMenu.IsVisible = true; break;
                case "Habitats": _habitatMenu.IsVisible = true; break;
                case "Animals": _animalMenu.IsVisible = true; break;
                case "Zookeepers": _zookeeperMenu.IsVisible = true; break;
            }

        }
        public void StartHabitatPlacement(string size)
        {
            _buildingsMenu.IsVisible = false;
            _habitatMenu.IsVisible = false;
            _animalMenu.IsVisible = false;
            _zookeeperMenu.IsVisible = false;

            if (size == "Medium")
            {
                _currentPlacement = PlacementMode.PlaceMediumHabitat;
                Console.WriteLine("Placement mode: Medium Habitat activated");
            }
        }
        public void ToggleTilePlacementMode()
        {
            _isPlacingRoadModeActive = !_isPlacingRoadModeActive;

            // Luk menuerne for visuel konsistens
            _buildingsMenu.IsVisible = false;
            _habitatMenu.IsVisible = false;
            _animalMenu.IsVisible = false;
            _zookeeperMenu.IsVisible = false;
        }
        public void StartAnimalPlacement(string animalType)
        {
            _buildingsMenu.IsVisible = false;
            _habitatMenu.IsVisible = false;
            _animalMenu.IsVisible = false;
            _zookeeperMenu.IsVisible = false;

            if (animalType == "Buffalo") // Du kan udvide til flere dyr senere
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Buffalo;
                Console.WriteLine("Placement mode: Buffalo activated");
            }
        }

    }
}
