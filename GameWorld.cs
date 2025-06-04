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

        public const int DEFAULT_SHOP_COST = 500;

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
        private Texture2D _shopPreviewTexture;
        private Texture2D _treePreviewTexture;
        private Texture2D _waterholePreviewTexture;


        //Menu

        private Texture2D _buttonTexture;
        private SpriteFont _menuFont;
        private Texture2D StartScreen;
        private List<Rectangle> _buttonRectangles;
        private List<string> _buttonLabels;
        private Vector2 _buttonSize = new Vector2(200, 50);

        // UI
        Button shopButton;
        Texture2D shopIconTexture;
        private ShopWindow _shopWindow;
        SubMenuWindow _buildingsMenu;
        SubMenuWindow _habitatMenu;
        SubMenuWindow _animalMenu;
        SubMenuWindow _zookeeperMenu;
        private StatDisplay _visitorDisplay;
        private StatDisplay _animalDisplay;

        private Texture2D _treeTexture;
        private List<Vector2> _staticTreePositions;
        private List<Vector2> placeTrees = new List<Vector2>();
        private List<Vector2> placeWaterholes = new List<Vector2>();



        private const int NUMBER_OF_STATIC_TREES = 1000;

        private List<Vector2> _boundaryFenceTilePositions;
        private HashSet<Vector2> _boundaryFenceTileCoordinates;

        private Texture2D _infoButtonTexture;
        private Texture2D _infoPanelTexture;
        private Texture2D _infoRibbonTexture;
        private Texture2D _infoIconTexture;

        private Button _infoButton;
        private bool _showInfoPanel = false;

        // Money Management
        private MoneyDisplay _moneyDisplay;

        public bool[,] WalkableMap { get; private set; }

        private enum PlacementMode // Enum til placering af ting
        {
            None,
            PlaceSmallHabitat,
            PlaceMediumHabitat,
            PlaceLargeHabitat,
            PlaceZookeeper,
            PlaceVisitorShop,
            PlaceAnimal_Buffalo,
            PlaceAnimal_Camel,
            PlaceAnimal_Wolf,
            PlaceAnimal_Bear,
            PlaceAnimal_Chimpanze,
            PlaceAnimal_Elephant,
            PlaceAnimal_Orangutan,
            PlaceAnimal_Polarbear,
            PlaceAnimal_Turtle,
            PlaceAnimal_Kangaroo,
            PlaceTree,
            PlaceWaterhole
        }
        public enum GameState // Enum til Menu
        {
            MainMenu,
            Playing,
            Loading,
            Exiting
        }
        private GameState _currentGameState = GameState.MainMenu;


        private PlacementMode _currentPlacement = PlacementMode.None;

        private bool isPlacingEnclosure = true;
        private List<Habitat> habitats;
        private List<Visitor> visitors;
        private List<Shop> shops;
        private int _nextHabitatId = 1;
        private int _nextAnimalId = 1;
        private int _nextVisitorId = 1;
        private int _nextZookeeperId = 1;
        private int _nextShopId = 1;

        private float _visitorSpawnTimer = 0f;
        private const float VISITOR_SPAWN_INTERVAL = 10.0f;
        private Vector2 _visitorSpawnTileCoord;
        private Vector2 _visitorExitTileCoord;
        private const int VISITOR_SPAWN_REWARD = 50;
        public Vector2 VisitorSpawnTileCoordinate => _visitorSpawnTileCoord;
        public Vector2 VisitorExitTileCoordinate => _visitorExitTileCoord;

        private Camera _camera;

        private bool _hasGameStarted = false;

        private bool _isFullscreen = false;

        private bool _isPlacingRoadModeActive = false;

        private List<Visitor> _visitorsToDespawn = new List<Visitor>();

        private EntityInfoPopup _entityInfoPopup;
        private IInspectableEntity _selectedEntity;

        private SaveButton saveButton;

        private bool IsMouseOverUI(Vector2 mousePosition)
        {
            Rectangle mouseRect = new Rectangle((int)mousePosition.X, (int)mousePosition.Y, 1, 1);

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

        public List<Shop> GetShops()
        {
            return shops;
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
            shops = new List<Shop>();
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
                    _graphics.PreferredBackBufferWidth - 260, // reposition af knapper
                    90
);
                _shopWindow.Reposition(newShopPos);
                Vector2 newShopButtonPos = new Vector2(
                    _graphics.PreferredBackBufferWidth - shopButton.GetWidth() - 10,
                    30
);
                shopButton.SetPosition(newShopButtonPos);

                Vector2 newIonfoButtonPos = new Vector2(
                    _graphics.PreferredBackBufferWidth - _infoButton.GetWidth() - 70,
                    30
);
                _infoButton.SetPosition(newIonfoButtonPos);

                Vector2 newSubMenuPos = new Vector2(
                _graphics.PreferredBackBufferWidth - 465, // reposition af knapper
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
            var (loadedHabitats, loadedShops, nextHabitatId, nextAnimalId, nextVisitorId, nextShopIdVal, nextZookeeperIdVal, loadedMoney, loadedScore) = DatabaseManager.Instance.LoadGame(Content);
            habitats = loadedHabitats;
            shops = loadedShops;
            _nextHabitatId = nextHabitatId;
            _nextAnimalId = nextAnimalId;
            _nextVisitorId = nextVisitorId;
            _nextShopId = nextShopIdVal;
            _nextZookeeperId = nextZookeeperIdVal;
            MoneyManager.Instance.Initialize(loadedMoney);
            ScoreManager.Instance.Score = loadedScore;

            foreach (var shop in shops)
            {
                Vector2 startTile = GameWorld.PixelToTile(shop.Position);
                for (int x = 0; x < shop.WidthInTiles; x++)
                {
                    for (int y = 0; y < shop.HeightInTiles; y++)
                    {
                        int tileX = (int)startTile.X + x;
                        int tileY = (int)startTile.Y + y;
                        if (tileX >= 0 && tileX < GRID_WIDTH && tileY >= 0 && tileY < GRID_HEIGHT)
                        {
                            WalkableMap[tileX, tileY] = false;
                        }
                    }
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");
            _fpsCounter = new FPSCounter(_font, _graphics);
            _habitatPreviewTexture = Content.Load<Texture2D>("fencesprite2");
            _shopPreviewTexture = Content.Load<Texture2D>("foodshopsprite_cut");
            SpriteFont uiFont = Content.Load<SpriteFont>("UIFont");


            // Menu fonts og textures
            _menuFont = Content.Load<SpriteFont>("MenuFont");
            _buttonTexture = Content.Load<Texture2D>("ButtonTexture");
            _buttonLabels = new List<string> { "Start Game", "Load Game", "Exit" };
            _buttonRectangles = new List<Rectangle>();
            StartScreen = Content.Load<Texture2D>("StartScreen");

            // Initialize shopButton textures and position

            Texture2D shopButtonBackgroundTexture = Content.Load<Texture2D>("Button_Blue");  
            Texture2D shopButtonIconTexture = Content.Load<Texture2D>("Regular_07");          

            Vector2 shopButtonPosition = new Vector2(GraphicsDevice.Viewport.Width - shopButtonBackgroundTexture.Width - 10, 30);
            shopButton = new Button(shopButtonBackgroundTexture, shopButtonIconTexture, shopButtonPosition);

            
            int buttonWidth = _buttonTexture.Width;
            int buttonHeight = _buttonTexture.Height;

            // Centrering af shopknap
            Vector2 centerShopButton = new Vector2(
                (GraphicsDevice.Viewport.Width - buttonWidth) / 2,
                (GraphicsDevice.Viewport.Height - buttonHeight) / 2
            );
            shopButton.Position = centerShopButton;

            
            int startY = 200;
            Vector2 _buttonSize = new Vector2(buttonWidth, buttonHeight);
            for (int i = 0; i < _buttonLabels.Count; i++)
            {
                _buttonRectangles.Add(new Rectangle(
                    (GraphicsDevice.Viewport.Width - (int)_buttonSize.X) / 2,
                    startY + i * 70,
                    (int)_buttonSize.X,
                    (int)_buttonSize.Y
                ));
            }

            //shop window textures og font
            Texture2D shopBackgroundTexture = Content.Load<Texture2D>("Button_Blue_9Slides");
            Texture2D buttonTexture = Content.Load<Texture2D>("Button_Blue_3Slides");
            Texture2D startButtonTexture = Content.Load<Texture2D>("Button_Blue_3Slides");

            
            SpriteFont font = Content.Load<SpriteFont>("UIFont");

            Vector2 subMenuPos = new Vector2(870, 75); // eller placer det ift. shopButton
            Vector2 saveButtonPos = new Vector2(5, 80);

            
            saveButton = new SaveButton(shopButtonBackgroundTexture, _font, saveButtonPos);


            // Menuliste
            string[] buildings = { "Tiles - 10", "Shop - 1.000", "Tree", "Waterhole" };
            string[] habitattype = { "Small - 5.000", "Medium - 10.000", "Large - 15.000" };
            string[] animals = { "Buffalo - 1.000", "Turtle - 5.000", "Chimpanze - 2.000", "Camel - 2.500", "Orangutan - 2.500", "Kangaroo - 2.500", "Wolf - 4.000", "Bear - 9.000", "Elephant - 8.000", "Polarbear - 10.000" };
            string[] zookeepers = { "Zookeeper - 5.000" };

            


            //Submenus
            _buildingsMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, uiFont, subMenuPos, buildings);
            _habitatMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, uiFont, subMenuPos, habitattype);
            _animalMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, uiFont, subMenuPos, animals);
            _zookeeperMenu = new SubMenuWindow(shopBackgroundTexture, buttonTexture, uiFont, subMenuPos, zookeepers);


            // Save game button
            Vector2 saveGamePosition = new Vector2(500, 90);
            


            //shop window
            Vector2 shopWindowPosition = new Vector2(1070, 90);
            _shopWindow = new ShopWindow(shopBackgroundTexture, buttonTexture, uiFont, shopWindowPosition);


            //Moneydisplay
            Texture2D moneyBackground = Content.Load<Texture2D>("Button_Blue_3Slides");
            Vector2 moneyPosition = new Vector2(10, 20);

            _moneyDisplay = new MoneyDisplay(
                uiFont,
                new Vector2(10, 10),
                Color.Black,
                1.25f,
                moneyBackground,
                new Vector2(0, 0),
                new Vector2(1f, 1f)
            );
            MoneyManager.Instance.Attach(_moneyDisplay);
            MoneyManager.Instance.Notify();

            Texture2D displayBg = Content.Load<Texture2D>("Button_Blue_3Slides");

            _visitorDisplay = new StatDisplay(uiFont, new Vector2(220, 10), Color.Black, 1.25f, displayBg, Vector2.Zero, new Vector2(1f, 1f));
            _animalDisplay = new StatDisplay(uiFont, new Vector2(430, 10), Color.Black, 1.25f, displayBg, Vector2.Zero, new Vector2(1f, 1f));

            _infoButtonTexture = Content.Load<Texture2D>("Button_Blue");
            _infoPanelTexture = Content.Load<Texture2D>("Button_Blue_9Slides");
            _infoRibbonTexture = Content.Load<Texture2D>("Ribbon_Blue_3Slides");
            _infoIconTexture = Content.Load<Texture2D>("info");

            Vector2 infoButtonPos = new Vector2(GraphicsDevice.Viewport.Width - _infoButtonTexture.Width - 70, 30);
            _infoButton = new Button(_infoButtonTexture, _infoIconTexture, infoButtonPos);

            _treePreviewTexture = Content.Load<Texture2D>("treegpt");
            _waterholePreviewTexture = Content.Load<Texture2D>("watergpt");

            // entity info popup
            _entityInfoPopup = new EntityInfoPopup(GraphicsDevice, _font);

            //tile textures
            tileTextures = new Texture2D[2];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");

            tileRenderer = new TileRenderer(tileTextures, TILE_SIZE);

            FenceRenderer.LoadContent(Content);
            InitializeBoundaryFences();

            //tree texture
            _treeTexture = Content.Load<Texture2D>("tree1");
            InitializeStaticTrees();

            // Load animal content for habitats
            foreach (var habitat in habitats)
            {
                habitat.LoadAnimalContent(Content);
            }
            Habitat.LoadContent(Content);

            //start game
            _startGameButtonTexture = startButtonTexture;
            _startGameButton = new Button(_startGameButtonTexture);

            Vector2 centerStartButton = new Vector2(
                (GraphicsDevice.Viewport.Width - _startGameButtonTexture.Width) / 2,
                (GraphicsDevice.Viewport.Height - _startGameButtonTexture.Height) / 2
            );
            _startGameButton.Position = centerStartButton;
        }

        MouseState prevMouseState;
        KeyboardState prevKeyboardState;
        private Texture2D _startGameButtonTexture;
        private Button _startGameButton;

        protected override void Update(GameTime gameTime)
        {

            MouseState mouse = Mouse.GetState();
            Point mousePos = mouse.Position;

            if (_currentGameState == GameState.MainMenu)
            {
                for (int i = 0; i < _buttonRectangles.Count; i++)
                {
                    Rectangle buttonRect = _buttonRectangles[i];

                    if (buttonRect.Contains(mousePos) &&
                        mouse.LeftButton == ButtonState.Pressed &&
                        prevMouseState.LeftButton == ButtonState.Pressed)
                    {
                        string label = _buttonLabels[i];

                        if (label == "Start Game")
                        {
                            _hasGameStarted = true;
                            _currentGameState = GameState.Playing;
                        }
                        else if (label == "Load Game")
                        {
                            Console.WriteLine("Load not implemented yet.");
                        }
                        else if (label == "Exit")
                        {
                            Exit();
                        }
                    }
                }
            }


            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _fpsCounter.Update(gameTime); //FPS
            _visitorDisplay.SetText($"Visitors: {visitors.Count}");
            _animalDisplay.SetText($"Animals: {habitats.Sum(h => h.GetAnimals().Count)}");

            KeyboardState keyboard = Keyboard.GetState();

            // F11 = Fullscreen
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






            if (keyboard.IsKeyDown(Keys.Z) && !prevKeyboardState.IsKeyDown(Keys.Z))
            {
                //place animal command
                var placeZookeeperCommand = new PlaceZookeeperCommand(worldMousePosition);
                CommandManager.Instance.ExecuteCommand(placeZookeeperCommand);
            }


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
                _visitorSpawnTimer = 0f; // Reset timer for spawn
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
                DatabaseManager.Instance.SaveGame();
            }


            if (keyboard.IsKeyDown(Keys.O) && !prevKeyboardState.IsKeyDown(Keys.O))
            {
                habitats.Clear();
                visitors.Clear();
                _nextHabitatId = 1;
                _nextAnimalId = 1;
                _nextVisitorId = 1;
                _nextZookeeperId = 1;
                CommandManager.Instance.Clear(); // Clear command
            }

            if (keyboard.IsKeyDown(Keys.M) && !prevKeyboardState.IsKeyDown(Keys.M))
            {
                MoneyManager.Instance.AddMoney(100000);
                Debug.WriteLine("Added $100,000 for debugging."); // Cheat code
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

            // Debug key = shop
            if (keyboard.IsKeyDown(Keys.J) && !prevKeyboardState.IsKeyDown(Keys.J))
            {
                var placeShopCommand = new PlaceShopCommand(_camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y)), 3, 3, DEFAULT_SHOP_COST);
                CommandManager.Instance.ExecuteCommand(placeShopCommand);
            }

            bool popupHandledClick = _entityInfoPopup.Update(mouse, prevMouseState);

            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed && !popupHandledClick)
            {
                Vector2 worldMousePos = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

                if (_currentPlacement == PlacementMode.PlaceSmallHabitat)
                {
                    var command = new PlaceHabitatCommand(worldMousePos, HabitatSizeType.Small, cost: 5000);
                    CommandManager.Instance.ExecuteCommand(command);
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceMediumHabitat)
                {
                    var command = new PlaceHabitatCommand(worldMousePos, HabitatSizeType.Medium, cost: 10000);
                    CommandManager.Instance.ExecuteCommand(command);
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceLargeHabitat)
                {
                    var command = new PlaceHabitatCommand(worldMousePos, HabitatSizeType.Large, cost: 15000);
                    CommandManager.Instance.ExecuteCommand(command);
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceVisitorShop)
                {
                    var placeShopCommand = new PlaceShopCommand(worldMousePos, 3, 3, DEFAULT_SHOP_COST);
                    CommandManager.Instance.ExecuteCommand(placeShopCommand);
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Buffalo)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Buffalo");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 1000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Orangutan)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Orangutan");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 2500);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Chimpanze)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Chimpanze");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 2000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Kangaroo)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Kangaroo");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 2500);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Elephant)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Elephant");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 8000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Camel)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Camel");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 2500);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Wolf)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Wolf");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 4000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Bear)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Bear");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 9000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Turtle)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Turtle");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 5000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceAnimal_Polarbear)
                {
                    int speciesId = DatabaseManager.Instance.GetSpeciesIdByName("Polarbear");
                    if (speciesId != -1)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, speciesId, cost: 10000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceTree)
                {
                    placeTrees.Add(worldMousePos); 
                    _currentPlacement = PlacementMode.None;
                }
                else if (_currentPlacement == PlacementMode.PlaceWaterhole)
                {
                    placeWaterholes.Add(worldMousePos);
                    _currentPlacement = PlacementMode.None;
                }

                else
                {
                    Vector2 worldMousePosForEntityCheck = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
                    bool entityClickedThisFrame = false;
                    IInspectableEntity clickedEntity = null;

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

                    if (_currentPlacement == PlacementMode.PlaceZookeeper)
                    {
                        var command = new PlaceZookeeperCommand(worldMousePos, cost: 5000);
                        CommandManager.Instance.ExecuteCommand(command);

                        _currentPlacement = PlacementMode.None;
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

            foreach (var shop in shops)
            {
                shop.Update(gameTime);
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

            // Shop ikon
            if (!shopButton.IsClicked)
            {
            }
            else
            {
                // Shop vindue
                _shopWindow.IsVisible = !_shopWindow.IsVisible;

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

            _infoButton.Update(Mouse.GetState(), prevMouseState);
            if (_infoButton.IsClicked)
            {
                _showInfoPanel = !_showInfoPanel;
            }

            saveButton.Update(gameTime, mouseState, prevMouseState);

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
                    _graphics.PreferredBackBufferWidth - 260, // repostion af knap
                    90
);
            _shopWindow.Reposition(newShopPos);
            Vector2 newShopButtonPos = new Vector2(
                _graphics.PreferredBackBufferWidth - shopButton.GetWidth() - 10,
                30
);
            shopButton.SetPosition(newShopButtonPos);

            Vector2 newIonfoButtonPos = new Vector2(
                _graphics.PreferredBackBufferWidth - _infoButton.GetWidth() - 70,
                30
);
            _infoButton.SetPosition(newIonfoButtonPos);
            shopButton.SetPosition(newShopButtonPos);
            Vector2 newSubMenuPos = new Vector2(
                _graphics.PreferredBackBufferWidth - 465, // repostion af knap
                90
);
            _buildingsMenu.Reposition(newSubMenuPos);
            _habitatMenu.Reposition(newSubMenuPos);
            _animalMenu.Reposition(newSubMenuPos);
            _zookeeperMenu.Reposition(newSubMenuPos);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_currentGameState == GameState.MainMenu)
            {

                //UI spritebatch
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

                if (StartScreen != null)
                {
                    _spriteBatch.Draw(StartScreen, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.White);
                }
                for (int i = 0; i < _buttonRectangles.Count; i++)
                {
                    Rectangle buttonRect = _buttonRectangles[i];
                    Color buttonColor = buttonRect.Contains(Mouse.GetState().Position) ? Color.Gray : Color.White;

                    _spriteBatch.Draw(_buttonTexture, buttonRect, buttonColor);

                    string label = _buttonLabels[i];
                    Vector2 size = _menuFont.MeasureString(label);
                    Vector2 position = new Vector2(
                        buttonRect.X + (buttonRect.Width - size.X) / 2,
                        buttonRect.Y + (buttonRect.Height - size.Y) / 2
                    );

                    _spriteBatch.DrawString(_menuFont, label, position, Color.Black);
                }

                _spriteBatch.End();

                return;
            }

            if (!_hasGameStarted)
            {
                // _spriteBatch.Draw(_menuBackgroundTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.Black);

                // menu knapper
                MouseState mouse = Mouse.GetState();
                Point mousePos = mouse.Position;

                for (int i = 0; i < _buttonLabels.Count; i++)
                {
                    Rectangle buttonRect = _buttonRectangles[i];
                    Color buttonColor = buttonRect.Contains(mousePos) ? Color.Gray : Color.White;

                    _spriteBatch.Draw(_buttonTexture, buttonRect, buttonColor);

                    string label = _buttonLabels[i];
                    Vector2 size = _menuFont.MeasureString(label);
                    Vector2 position = new Vector2(
                        buttonRect.X + (buttonRect.Width - size.X) / 2,
                        buttonRect.Y + (buttonRect.Height - size.Y) / 2
                    );

                    _spriteBatch.DrawString(_menuFont, label, position, Color.Black);

                }


                _spriteBatch.End(); // end ui
                return;
            }



            // world/game drawing
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Matrix transform = _camera.GetTransformMatrix();

            _spriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

            // tegn map
            if (_currentGameState == GameState.Playing)
            {
                tileRenderer.Draw(_spriteBatch, map);
            }

            // Draw placed trees and waterholes
            foreach (var tree in placeTrees)
            {
                _spriteBatch.Draw(_treePreviewTexture, tree, Color.White);
            }
            foreach (var waterhole in placeWaterholes)
            {
                _spriteBatch.Draw(_waterholePreviewTexture, waterhole, Color.White);
            }

            // static trees 
            if (_treeTexture != null && _staticTreePositions != null)
            {
                foreach (var treePos in _staticTreePositions)
                {
                    _spriteBatch.Draw(_treeTexture, treePos, Color.White);
                }
            }

            // Boundary fences
            if (_boundaryFenceTilePositions != null && _boundaryFenceTilePositions.Count > 0)
            {
                FenceRenderer.Draw(_spriteBatch, _boundaryFenceTilePositions, _boundaryFenceTileCoordinates, 2.67f);
            }

            // Road preview
            if (_isPlacingRoadModeActive)
            {
                MouseState mouse = Mouse.GetState();
                Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
                Vector2 tilePreviewPosition = PixelToTile(worldMousePosition);

                if (tilePreviewPosition.X >= 0 && tilePreviewPosition.X < GRID_WIDTH &&
                    tilePreviewPosition.Y >= 0 && tilePreviewPosition.Y < GRID_HEIGHT)
                {
                    Rectangle destinationRectangle = new Rectangle(
                        (int)tilePreviewPosition.X * TILE_SIZE,
                        (int)tilePreviewPosition.Y * TILE_SIZE,
                        TILE_SIZE,
                        TILE_SIZE
                    );
                    _spriteBatch.Draw(tileTextures[ROAD_TEXTURE_INDEX], destinationRectangle, Color.White * 0.5f);
                }
            }

            // Habitats
            foreach (var habitat in habitats)
            {
                habitat.Draw(_spriteBatch);
            }

            // Shops
            foreach (var shop in shops)
            {
                shop.Draw(_spriteBatch);
            }

            // Visitors
            foreach (var visitor in visitors)
            {
                visitor.Draw(_spriteBatch);
            }

            // Habitat placement preview
            if (_currentPlacement == PlacementMode.PlaceSmallHabitat ||
                _currentPlacement == PlacementMode.PlaceMediumHabitat ||
                _currentPlacement == PlacementMode.PlaceLargeHabitat)
            {
                Vector2 mousePos = Mouse.GetState().Position.ToVector2();
                Vector2 worldMouse = _camera.ScreenToWorld(mousePos);

                Vector2 snappedTile = new Vector2(
                    (int)(worldMouse.X / TILE_SIZE),
                    (int)(worldMouse.Y / TILE_SIZE)
                );

                Vector2 snappedPos = new Vector2(
                    snappedTile.X * TILE_SIZE,
                    snappedTile.Y * TILE_SIZE
                );

                int habitatRadiusTiles = 0;
                if (_currentPlacement == PlacementMode.PlaceSmallHabitat) habitatRadiusTiles = 2;
                else if (_currentPlacement == PlacementMode.PlaceMediumHabitat) habitatRadiusTiles = 4;
                else if (_currentPlacement == PlacementMode.PlaceLargeHabitat) habitatRadiusTiles = 6;

                int enclosureDiameterTiles = (habitatRadiusTiles * 2) + 1;
                float previewPixelSize = enclosureDiameterTiles * TILE_SIZE * 1.25f;

                Vector2 previewTopLeft = snappedPos + new Vector2(TILE_SIZE / 2) - new Vector2(previewPixelSize / 2);

                Rectangle destRect = new Rectangle(
                    (int)previewTopLeft.X,
                    (int)previewTopLeft.Y,
                    (int)previewPixelSize,
                    (int)previewPixelSize
                );

                _spriteBatch.Draw(_habitatPreviewTexture, destRect, Color.White * 0.5f);
            }

            // Shop placering preview
            if (_currentPlacement == PlacementMode.PlaceVisitorShop && _shopPreviewTexture != null)
            {
                MouseState mouse = Mouse.GetState();
                Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

                int shopWidthInTiles = 3;
                int shopHeightInTiles = 3;

                Vector2 cursorTile = PixelToTile(worldMousePosition);

                int previewTopLeftTileX = (int)cursorTile.X - (shopWidthInTiles / 2);
                int previewTopLeftTileY = (int)cursorTile.Y - (shopHeightInTiles / 2);

                Vector2 snappedDrawPosition = new Vector2(
                    previewTopLeftTileX * TILE_SIZE,
                    previewTopLeftTileY * TILE_SIZE
                );

                Rectangle destinationRectangle = new Rectangle(
                    (int)snappedDrawPosition.X,
                    (int)snappedDrawPosition.Y,
                    shopWidthInTiles * TILE_SIZE,
                    shopHeightInTiles * TILE_SIZE
                );

                _spriteBatch.Draw(_shopPreviewTexture, destinationRectangle, Color.White * 0.5f);
            }
            if (_currentPlacement == PlacementMode.PlaceTree && _treePreviewTexture != null)
            {
                Vector2 worldMouse = _camera.ScreenToWorld(Mouse.GetState().Position.ToVector2());
                Vector2 snappedPos = new Vector2(
                    ((int)(worldMouse.X / TILE_SIZE)) * TILE_SIZE,
                    ((int)(worldMouse.Y / TILE_SIZE)) * TILE_SIZE
                );

                Rectangle previewRect = new Rectangle((int)snappedPos.X, (int)snappedPos.Y, _treePreviewTexture.Width, _treePreviewTexture.Height);
                _spriteBatch.Draw(_treePreviewTexture, previewRect, Color.White * 0.5f);
            }

            if (_currentPlacement == PlacementMode.PlaceWaterhole && _waterholePreviewTexture != null)
            {
                Vector2 worldMouse = _camera.ScreenToWorld(Mouse.GetState().Position.ToVector2());
                Vector2 snappedPos = new Vector2(
                    ((int)(worldMouse.X / TILE_SIZE)) * TILE_SIZE,
                    ((int)(worldMouse.Y / TILE_SIZE)) * TILE_SIZE
                );

                Rectangle previewRect = new Rectangle((int)snappedPos.X, (int)snappedPos.Y, _waterholePreviewTexture.Width, _waterholePreviewTexture.Height);
                _spriteBatch.Draw(_waterholePreviewTexture, previewRect, Color.White * 0.5f);
            }


            _spriteBatch.End(); // ✅ END spritebach gameworld

            // UI
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_isPlacingRoadModeActive)
            {
                Vector2 infoPosition = new Vector2(550, 650);
                _spriteBatch.DrawString(_font, "Press Mouse 1 to place tiles", infoPosition, Color.Yellow);
                _spriteBatch.DrawString(_font, "Press P to exit tile mode", infoPosition + new Vector2(0, 25), Color.Yellow);
            }


            saveButton.Draw(_spriteBatch);

            _fpsCounter.Draw(_spriteBatch);

            string instructions = "Press 'B' for spawning visitor\nPress 'S' to save\nPress 'O' to clear everything\nPress 'M' to add $100k (debug)\nPress 'F11' to toggle fullscreen\nUse middle mouse or arrow keys to move camera\nUse mouse wheel to zoom\nCtrl+Z to undo, Ctrl+Y to redo";
            Vector2 textPosition = new Vector2(10, _graphics.PreferredBackBufferHeight - 200);
            _spriteBatch.DrawString(_font, instructions, textPosition, Color.White);

            _moneyDisplay.Draw(_spriteBatch);
            _visitorDisplay.Draw(_spriteBatch);
            _animalDisplay.Draw(_spriteBatch);

            Vector2 undoRedoPosition = new Vector2(10, 75);
            string undoRedoText = $"Undo: {CommandManager.Instance.GetUndoDescription()}\nRedo: {CommandManager.Instance.GetRedoDescription()}";
            _spriteBatch.DrawString(_font, undoRedoText, undoRedoPosition, Color.LightBlue);




            // Tegn shop knappen
            shopButton.Draw(_spriteBatch);

            // UI windows and buttons
            shopButton.Draw(_spriteBatch);
            _shopWindow.Draw(_spriteBatch);
            _buildingsMenu.Draw(_spriteBatch);
            _habitatMenu.Draw(_spriteBatch);
            _animalMenu.Draw(_spriteBatch);
            _zookeeperMenu.Draw(_spriteBatch);
            _entityInfoPopup.Draw(_spriteBatch);

            _infoButton.Draw(_spriteBatch);

            if (_showInfoPanel)
            {
                // Info panel tekstlinjer
                string[] lines = new[]
                {
        "Remember to keep your visitors happy!",
        "They like to see happy animals, and have easy access to food when they're hungry.",
        "",
        "Remember to hire zookeepers to look out for your animals!",
        "",
        "You can undo and redo your actions by pressing Ctrl + Z and Ctrl + Y"
    };

                // Beregn bredeste linje
                float maxWidth = lines.Max(line => _font.MeasureString(line).X);
                float lineHeight = _font.LineSpacing;
                int lineCount = lines.Length;

                // Padding
                int horizontalPadding = 75;
                int verticalPadding = 100;

                // Samlet panelstørrelse – med ekstra plads
                Vector2 panelSize = new Vector2(
                    maxWidth + horizontalPadding * 2 + 60,
                    lineCount * lineHeight + verticalPadding + 30
                );

                // Center position
                Vector2 panelPos = new Vector2(
                    (_graphics.PreferredBackBufferWidth - panelSize.X) / 2,
                    (_graphics.PreferredBackBufferHeight - panelSize.Y) / 2
                );

                // Tegn baggrund som rectangle
                Rectangle panelRect = new Rectangle(
                    (int)panelPos.X,
                    (int)panelPos.Y,
                    (int)panelSize.X,
                    (int)panelSize.Y
                );

                _spriteBatch.Draw(_infoPanelTexture, panelRect, Color.White);

                // Ribbon (øverst centreret på boksen)
                Vector2 ribbonPos = new Vector2(
                    panelRect.X + (panelRect.Width - _infoRibbonTexture.Width) / 2,
                    panelRect.Y + 10
                );
                _spriteBatch.Draw(_infoRibbonTexture, ribbonPos, Color.White);
                ScoreManager.Instance.Draw(_spriteBatch, _font, ribbonPos + new Vector2(_infoRibbonTexture.Width / 2-5f, _infoRibbonTexture.Height / 2 - 10f));

                // Tekst – start lidt under ribbon
                Vector2 textStart = new Vector2(panelRect.X + horizontalPadding, ribbonPos.Y + _infoRibbonTexture.Height + 20);

                for (int i = 0; i < lines.Length; i++)
                {
                    Vector2 linePos = textStart + new Vector2(0, i * lineHeight);
                    _spriteBatch.DrawString(_font, lines[i], linePos, Color.Black);
                }
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public int GetNextAnimalId()
        {
            return _nextAnimalId++; // unique animal ID
        }

        public int GetNextHabitatId()
        {
            return _nextHabitatId++; // unique habitat ID
        }

        public int GetNextZookeeperId()
        {
            return _nextZookeeperId++; // unique zookeeper ID
        }

        public int GetNextShopId()
        {
            return _nextShopId++; // unique shop ID
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

                var placeRoadCommand = new PlaceRoadCommand(tilePosition, originalTile, 10);
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
            HideAllMenus();

            if (size == "Small - 5.000")
            {
                _currentPlacement = PlacementMode.PlaceSmallHabitat;
                Console.WriteLine("Placement mode: Small Habitat activated");
            }
            else if (size == "Medium - 10.000")
            {
                _currentPlacement = PlacementMode.PlaceMediumHabitat;
                Console.WriteLine("Placement mode: Medium Habitat activated");
            }
            else if (size == "Large - 15.000")
            {
                _currentPlacement = PlacementMode.PlaceLargeHabitat;
                Console.WriteLine("Placement mode: Large Habitat activated");
            }
        }

        public void HideAllMenus()
        {
            if (_shopWindow != null) _shopWindow.IsVisible = false;
            if (_buildingsMenu != null) _buildingsMenu.IsVisible = false;
            if (_habitatMenu != null) _habitatMenu.IsVisible = false;
            if (_animalMenu != null) _animalMenu.IsVisible = false;
            if (_zookeeperMenu != null) _zookeeperMenu.IsVisible = false;
        }

        public void StartShopPlacement(string shopType)
        {
            HideAllMenus();

            if (shopType == "Shop - 1.000")
            {
                _currentPlacement = PlacementMode.PlaceVisitorShop;
                Debug.WriteLine("Placement mode: Visitor Shop activated");
            }
        }
        public void ToggleTilePlacementMode()
        {
            _isPlacingRoadModeActive = !_isPlacingRoadModeActive;
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

            //int speciesIdToPlace = DatabaseManager.Instance.GetSpeciesIdByName(animalType);
            //if (speciesIdToPlace == -1) 
            //{
            //    Debug.WriteLine($"Animal placement failed: Species '{animalType}' not found in database.");
            //    _currentPlacement = PlacementMode.None; 
            //    return; 
            //}           

            if (animalType == "Buffalo - 1.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Buffalo;
            }
            if (animalType == "Kangaroo - 2.500") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Kangaroo;
            }
            if (animalType == "Polarbear - 10.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Polarbear;
            }
            if (animalType == "Bear - 9.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Bear;
            }
            if (animalType == "Chimpanze - 2.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Chimpanze;
            }
            if (animalType == "Elephant - 8.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Elephant;
            }
            if (animalType == "Orangutan - 2.500") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Orangutan;
            }
            if (animalType == "Turtle - 5.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Turtle;
            }
            if (animalType == "Wolf - 4.000") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Wolf;
            }
            if (animalType == "Camel - 2.500") 
            {
                _currentPlacement = PlacementMode.PlaceAnimal_Camel;
            }
        }

        public void StartZookeeperPlacement(string name)
        {
            _buildingsMenu.IsVisible = false;
            _habitatMenu.IsVisible = false;
            _animalMenu.IsVisible = false;
            _zookeeperMenu.IsVisible = false;

            if (name == "Zookeeper - 5.000")
            {
                _currentPlacement = PlacementMode.PlaceZookeeper;
                Console.WriteLine("Placement mode: Zookeeper activated");
            }
        }
        public void StartTreePlacement()
        {
            HideAllMenus();
            _currentPlacement = PlacementMode.PlaceTree;
            Console.WriteLine("Placement mode: Tree activated");
        }

        public void StartWaterholePlacement()
        {
            HideAllMenus();
            _currentPlacement = PlacementMode.PlaceWaterhole;
            Console.WriteLine("Placement mode: Waterhole activated");
        }
    }
}
