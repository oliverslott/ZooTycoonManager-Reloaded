using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using ZooTycoonManager.Commands;
using ZooTycoonManager.Components;
using ZooTycoonManager.Enums;
using ZooTycoonManager.Interfaces;
using ZooTycoonManager.UI;

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

        public float deltaTime;

        private static GameWorld _instance;
        private static readonly object _lock = new object();
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        public Map map { get; private set; }
        TileRenderer tileRenderer;
        private Texture2D[] tileTextures;
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

        // UI
        //Button shopButton;
        Texture2D shopIconTexture;
        GameObject _shopWindow;
        GameObject _buildingsMenu;
        GameObject _habitatMenu;
        GameObject _animalMenu;
        GameObject _zookeeperMenu;
        GameObject _infoPanel;

        private Texture2D _treeTexture;
        private List<Vector2> _staticTreePositions;
        private List<Vector2> placeTrees = new List<Vector2>();
        private List<Vector2> placeWaterholes = new List<Vector2>();



        private const int NUMBER_OF_STATIC_TREES = 1000;

        private List<Vector2> _boundaryFenceTilePositions;
        private HashSet<Vector2> _boundaryFenceTileCoordinates;

        private Texture2D _infoRibbonTexture;
        private Texture2D _infoIconTexture;


        public bool[,] WalkableMap { get; private set; }

        public HashSet<(int x, int y)> RoadTiles { get; set; } = new HashSet<(int x, int y)>();

        private enum PlacementMode // Enum til placering af ting
        {
            None,
            PlaceSmallHabitat,
            PlaceMediumHabitat,
            PlaceLargeHabitat,
            PlaceZookeeper,
            PlaceVisitorShop,
            PlaceAnimal,
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
        private AnimalTypes _animalTypeToPlace;
        private int _animalCostToPlace;

        private List<GameObject> gameObjects = new List<GameObject>();
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
        private bool _ignoreNextMouseClick = false;

        private List<GameObject> _gameObjectsToRemove = new List<GameObject>();

        private GameObject _entityInfoPopupObject;
        private IInspectableEntity _selectedEntity;

        private PlacementMode _previousPlacement;
        private bool _wasPlacingRoadModeActive;

        public List<GameObject> GetAnimals()
        {
            return gameObjects.Where(x => x.TryGetComponent<AnimalComponent>(out _)).ToList();
        }

        public List<GameObject> GetHabitats()
        {
            return gameObjects.Where(x => x.TryGetComponent<HabitatComponent>(out _)).ToList();
        }

        public List<GameObject> GetVisitors()
        {
            return gameObjects.Where(x => x.TryGetComponent<VisitorComponent>(out _)).ToList();
        }

        public List<GameObject> GetShops()
        {
            return gameObjects.Where(x => x.TryGetComponent<ShopComponent>(out _)).ToList();
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
            //var (loadedHabitats, loadedShops, nextHabitatId, nextAnimalId, nextVisitorId, nextShopIdVal, nextZookeeperIdVal, loadedMoney, loadedScore) = DatabaseManager.Instance.LoadGame(Content);
            //foreach(var loadedHabitat in loadedHabitats)
            //{
            //    gameObjects.Add(loadedHabitat);
            //}
            //foreach (var loadedShop in loadedShops)
            //{
            //    gameObjects.Add(loadedShop);
            //    Vector2 startTile = GameWorld.PixelToTile(loadedShop.Position);
            //    for (int x = 0; x < loadedShop.WidthInTiles; x++)
            //    {
            //        for (int y = 0; y < loadedShop.HeightInTiles; y++)
            //        {
            //            int tileX = (int)startTile.X + x;
            //            int tileY = (int)startTile.Y + y;
            //            if (tileX >= 0 && tileX < GRID_WIDTH && tileY >= 0 && tileY < GRID_HEIGHT)
            //            {
            //                WalkableMap[tileX, tileY] = false;
            //            }
            //        }
            //    }
            //}

            //_nextHabitatId = nextHabitatId;
            //_nextAnimalId = nextAnimalId;
            //_nextVisitorId = nextVisitorId;
            //_nextShopId = nextShopIdVal;
            //_nextZookeeperId = nextZookeeperIdVal;
            //MoneyManager.Instance.Initialize(loadedMoney);
            //ScoreManager.Instance.Score = loadedScore;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");
            _habitatPreviewTexture = Content.Load<Texture2D>("fencesprite2");
            _shopPreviewTexture = Content.Load<Texture2D>("foodshopsprite_cut");

            //foreach(var gameObject in gameObjects)
            //{
            //    gameObject.LoadContent();
            //}

            // Menu fonts og textures
            _menuFont = Content.Load<SpriteFont>("MenuFont");
            _buttonTexture = Content.Load<Texture2D>("ButtonTexture");
            _buttonLabels = new List<string> { "Start Game", "Load Game", "Exit" };
            _buttonRectangles = new List<Rectangle>();
            StartScreen = Content.Load<Texture2D>("StartScreen");





            var buildingsMenuPos = new Vector2(GraphicsDevice.Viewport.Width - 500, 100);

            var buildingsBtns = new List<GameObject>();
            var tilesBtn = EntityFactory.CreateButton(new Vector2(buildingsMenuPos.X, buildingsMenuPos.Y - 50), "Tiles - 10", () => { _isPlacingRoadModeActive = true; ToggleShop(); });
            var shopBtn = EntityFactory.CreateButton(new Vector2(buildingsMenuPos.X, buildingsMenuPos.Y), "Shop - 1.000", () => { StartShopPlacement("Shop - 1.000"); });
            var treeBtn = EntityFactory.CreateButton(new Vector2(buildingsMenuPos.X, buildingsMenuPos.Y + 50), "Tree", () => { StartTreePlacement(); });
            var waterholeBtn = EntityFactory.CreateButton(new Vector2(buildingsMenuPos.X, buildingsMenuPos.Y + 100), "Waterhole", () => { StartWaterholePlacement(); });

            buildingsBtns.Add(tilesBtn);
            buildingsBtns.Add(shopBtn);
            buildingsBtns.Add(treeBtn);
            buildingsBtns.Add(waterholeBtn);

            _buildingsMenu = Instantiate(EntityFactory.CreateMenu(new Vector2(buildingsMenuPos.X, buildingsMenuPos.Y), buildingsBtns));
            _buildingsMenu.IsActive = false;

            var habitatsMenuPos = new Vector2(GraphicsDevice.Viewport.Width - 500, 100);
            var habitatsBtns = new List<GameObject>();
            var smallBtn = EntityFactory.CreateButton(new Vector2(habitatsMenuPos.X, habitatsMenuPos.Y - 50), "Small - 5.000", () => { StartHabitatPlacement("Small - 5.000"); });
            var mediumBtn = EntityFactory.CreateButton(new Vector2(habitatsMenuPos.X, habitatsMenuPos.Y), "Medium - 10.000", () => { StartHabitatPlacement("Medium - 10.000"); });
            var largeBtn = EntityFactory.CreateButton(new Vector2(habitatsMenuPos.X, buildingsMenuPos.Y + 50), "Large - 15.000", () => { StartHabitatPlacement("Large - 15.000"); });

            habitatsBtns.Add(smallBtn);
            habitatsBtns.Add(mediumBtn);
            habitatsBtns.Add(largeBtn);

            _habitatMenu = Instantiate(EntityFactory.CreateMenu(new Vector2(habitatsMenuPos.X, habitatsMenuPos.Y), habitatsBtns));
            _habitatMenu.IsActive = false;

            var shopPos = new Vector2(GraphicsDevice.Viewport.Width - 200, 150);

            var menuBtns = new List<GameObject>();
            var BuildingsBtn = EntityFactory.CreateButton(new Vector2(shopPos.X, shopPos.Y - 50), "Buildings", () => { ShowSubMenu("Buildings"); });
            var HabitatsBtn = EntityFactory.CreateButton(new Vector2(shopPos.X, shopPos.Y), "Habitats", () => { ShowSubMenu("Habitats"); });
            var AnimalsBtn = EntityFactory.CreateButton(new Vector2(shopPos.X, shopPos.Y + 50), "Animals", () => { ShowSubMenu("Animals"); });
            var ZookeepersBtn = EntityFactory.CreateButton(new Vector2(shopPos.X, shopPos.Y + 100), "Zookeepers", () => { ShowSubMenu("Zookeepers"); });

            menuBtns.Add(BuildingsBtn);
            menuBtns.Add(HabitatsBtn);
            menuBtns.Add(AnimalsBtn);
            menuBtns.Add(ZookeepersBtn);

            _shopWindow = Instantiate(EntityFactory.CreateMenu(new Vector2(shopPos.X, shopPos.Y), menuBtns));
            _shopWindow.IsActive = false;

            Vector2 shopButtonPosition = new Vector2(GraphicsDevice.Viewport.Width - 100, 30);
            Instantiate(EntityFactory.CreateButton(shopButtonPosition, "Shop", ToggleShop, ButtonSize.Small));

            Instantiate(EntityFactory.CreateButtonWithIcon(new Vector2(GraphicsDevice.Viewport.Width - 170, 30), "info", ToggleInfoPanel, ButtonSize.Small));

            Instantiate(EntityFactory.CreateMoneyUI(new Vector2(20, 5)));
            Instantiate(EntityFactory.CreateVisitorUI(new Vector2(220, 5)));
            Instantiate(EntityFactory.CreateAnimalUI(new Vector2(420, 5)));

            // Initialize shopButton textures and position

            Texture2D shopButtonBackgroundTexture = Content.Load<Texture2D>("Button_Blue");
            //Texture2D shopButtonIconTexture = Content.Load<Texture2D>("Regular_07");          

            //Vector2 shopButtonPosition = new Vector2(GraphicsDevice.Viewport.Width - shopButtonBackgroundTexture.Width - 10, 30);
            //shopButton = new Button(shopButtonBackgroundTexture, shopButtonIconTexture, shopButtonPosition);


            int buttonWidth = _buttonTexture.Width;
            int buttonHeight = _buttonTexture.Height;

            // Centrering af shopknap
            Vector2 centerShopButton = new Vector2(
                (GraphicsDevice.Viewport.Width - buttonWidth) / 2,
                (GraphicsDevice.Viewport.Height - buttonHeight) / 2
            );
            //shopButton.Position = centerShopButton;


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


            // Menuliste
            string[] buildings = { "Tiles - 10", "Shop - 1.000", "Tree", "Waterhole" };
            string[] habitattype = { "Small - 5.000", "Medium - 10.000", "Large - 15.000" };
            string[] animals = { "Buffalo - 1.000", "Turtle - 5.000", "Chimpanze - 2.000", "Camel - 2.500", "Orangutan - 2.500", "Kangaroo - 2.500", "Wolf - 4.000", "Bear - 9.000", "Elephant - 8.000", "Polarbear - 10.000" };
            string[] zookeepers = { "Zookeeper - 5.000" };

            //shop window
            Vector2 shopWindowPosition = new Vector2(1070, 90);
            //_shopWindow = new ShopWindow(shopBackgroundTexture, buttonTexture, uiFont, shopWindowPosition);


            //Moneydisplay
            Texture2D moneyBackground = Content.Load<Texture2D>("Button_Blue_3Slides");
            Vector2 moneyPosition = new Vector2(10, 20);

            Texture2D displayBg = Content.Load<Texture2D>("Button_Blue_3Slides");

            _infoRibbonTexture = Content.Load<Texture2D>("Ribbon_Blue_3Slides");
            _infoIconTexture = Content.Load<Texture2D>("info");

            _treePreviewTexture = Content.Load<Texture2D>("treegpt");
            _waterholePreviewTexture = Content.Load<Texture2D>("watergpt");

            // entity info popup
            _entityInfoPopupObject = Instantiate(EntityFactory.CreateEntityInfoPopup());

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

            var animalMenuPos = new Vector2(GraphicsDevice.Viewport.Width - 500, 200);
            var animalsBtns = new List<GameObject>();

            for (int i = 0; i < animals.Length; i++)
            {
                string animal = animals[i];
                var btn = EntityFactory.CreateButton(new Vector2(animalMenuPos.X, animalMenuPos.Y + (-50 + 50 * i)), animal, () => { StartAnimalPlacement(animal); });
                animalsBtns.Add(btn);
            }

            _animalMenu = Instantiate(EntityFactory.CreateMenu(new Vector2(animalMenuPos.X, animalMenuPos.Y), animalsBtns));
            _animalMenu.IsActive = false;

            var zookeeperMenuPos = new Vector2(GraphicsDevice.Viewport.Width - 300, 200);
            var zookeeperBtns = new List<GameObject>();

            for (int i = 0; i < zookeepers.Length; i++)
            {
                string zookeeper = zookeepers[i];
                var btn = EntityFactory.CreateButton(new Vector2(zookeeperMenuPos.X, zookeeperMenuPos.Y - (50 + 50 * i)), zookeeper, () => { StartZookeeperPlacement(zookeeper); });
                zookeeperBtns.Add(btn);
            }

            _zookeeperMenu = Instantiate(EntityFactory.CreateMenu(new Vector2(zookeeperMenuPos.X, zookeeperMenuPos.Y), zookeeperBtns));
            _zookeeperMenu.IsActive = false;

            Instantiate(EntityFactory.CreateFPSCounter(new Vector2(_graphics.PreferredBackBufferWidth - 60, 20)));

            Instantiate(EntityFactory.CreateButton(new Vector2(50, 120), "Save", () => { }, ButtonSize.Small));

            _infoPanel = Instantiate(EntityFactory.CreateInfoPanel());
        }

        public void ToggleShop()
        {
            _shopWindow.IsActive = !_shopWindow.IsActive;

            _buildingsMenu.IsActive = false;
            _habitatMenu.IsActive = false;
            _animalMenu.IsActive = false;
            _zookeeperMenu.IsActive = false;
        }

        private void ToggleInfoPanel()
        {
            _infoPanel.IsActive = !_infoPanel.IsActive;
        }

        MouseState prevMouseState;
        KeyboardState prevKeyboardState;

        protected override void Update(GameTime gameTime)
        {
            deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            foreach (var gameObject in gameObjects)
            {
                gameObject.Update(gameTime);
            }

            if ((_currentPlacement != PlacementMode.None && _previousPlacement == PlacementMode.None) || (_isPlacingRoadModeActive && !_wasPlacingRoadModeActive))
            {
                _ignoreNextMouseClick = true;
            }

            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.Escape) && !prevKeyboardState.IsKeyDown(Keys.Escape))
            {
                _isPlacingRoadModeActive = false;
                _currentPlacement = PlacementMode.None;
            }

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

            if (keyboard.IsKeyDown(Keys.B) && !prevKeyboardState.IsKeyDown(Keys.B))
            {
                Instantiate(EntityFactory.CreateVisitor(_camera.ScreenToWorld(mouse.Position.ToVector2())));
            }

            if (mouse.RightButton == ButtonState.Pressed && prevMouseState.RightButton != ButtonState.Pressed)
            {
                var newPos = PixelToTile(_camera.ScreenToWorld(mouse.Position.ToVector2()));
                var visitor = GetVisitors()[0];
                bool[,] tempWalkableMap = new bool[100, 100];
                for (int x = 0; x < 100; x++)
                    for (int y = 0; y < 100; y++)
                        tempWalkableMap[x, y] = true;

                visitor.GetComponent<MovableComponent>().PathfindTo(newPos, tempWalkableMap);
            }

            Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

            bool animalsExist = GetAnimals().Count > 0;
            if (animalsExist)
            {
                _visitorSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_visitorSpawnTimer >= VISITOR_SPAWN_INTERVAL)
                {
                    _visitorSpawnTimer = 0f;

                    _nextVisitorId++;
                    gameObjects.Add(EntityFactory.CreateVisitor(_visitorSpawnTileCoord));

                    MoneyManager.Instance.AddMoney(VISITOR_SPAWN_REWARD);
                    Debug.WriteLine($"Visitor spawned at {_visitorSpawnTileCoord}. Added ${VISITOR_SPAWN_REWARD}.");
                }
            }
            else
            {
                _visitorSpawnTimer = 0f; // Reset timer for spawn
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

            // This new block handles resetting the ignore flag on mouse release.
            if (mouse.LeftButton == ButtonState.Released)
            {
                if (_ignoreNextMouseClick)
                {
                    _ignoreNextMouseClick = false;
                }
            }

            bool popupHandledClick = _entityInfoPopupObject.GetComponent<EntityInfoPopupComponent>().PopupHandledClick;

            // This consolidated block handles all left-click actions.
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (_ignoreNextMouseClick)
                {
                    // The click that initiated placement mode is being ignored until the button is released.
                }
                else if (_isPlacingRoadModeActive)
                {
                    // If we are in road placement mode, place tiles continuously.
                    var placeRoadCommand = new PlaceRoadCommand(PixelToTile(worldMousePosition), 10);
                    CommandManager.Instance.ExecuteCommand(placeRoadCommand);
                }
                else if (prevMouseState.LeftButton != ButtonState.Pressed && !popupHandledClick)
                {
                    // This handles single-click actions for all other placement modes.
                    Vector2 worldMousePos = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

                    if (_currentPlacement == PlacementMode.PlaceSmallHabitat)
                    {
                        var command = new PlaceHabitatCommand(worldMousePos, HabitatSizeType.Small, cost: 5000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    else if (_currentPlacement == PlacementMode.PlaceMediumHabitat)
                    {
                        var command = new PlaceHabitatCommand(worldMousePos, HabitatSizeType.Medium, cost: 10000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    else if (_currentPlacement == PlacementMode.PlaceLargeHabitat)
                    {
                        var command = new PlaceHabitatCommand(worldMousePos, HabitatSizeType.Large, cost: 15000);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    else if (_currentPlacement == PlacementMode.PlaceVisitorShop)
                    {
                        var placeShopCommand = new PlaceShopCommand(worldMousePos, 3, 3, DEFAULT_SHOP_COST);
                        CommandManager.Instance.ExecuteCommand(placeShopCommand);
                    }
                    else if (_currentPlacement == PlacementMode.PlaceAnimal)
                    {
                        var command = new PlaceAnimalCommand(worldMousePos, _animalTypeToPlace, cost: _animalCostToPlace);
                        CommandManager.Instance.ExecuteCommand(command);
                    }
                    else if (_currentPlacement == PlacementMode.PlaceTree)
                    {
                        placeTrees.Add(worldMousePos);
                    }
                    else if (_currentPlacement == PlacementMode.PlaceWaterhole)
                    {
                        placeWaterholes.Add(worldMousePos);
                    }

                    else
                    {
                        Vector2 worldMousePosForEntityCheck = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
                        bool entityClickedThisFrame = false;
                        IInspectableEntity clickedEntity = null;

                        if (_currentPlacement == PlacementMode.PlaceZookeeper)
                        {
                            var command = new PlaceZookeeperCommand(worldMousePos, cost: 5000);
                            CommandManager.Instance.ExecuteCommand(command);

                            _currentPlacement = PlacementMode.None;
                        }

                        if (!entityClickedThisFrame)
                        {
                            //foreach (Visitor visitor in gameObjects.Where(x=>x is Visitor).Cast<Visitor>())
                            //{
                            //    Rectangle visitorBoundingBox = new Rectangle((int)(visitor.Position.X - 16), (int)(visitor.Position.Y - 16), 32, 32);
                            //    if (visitorBoundingBox.Contains(worldMousePosForEntityCheck))
                            //    {
                            //        clickedEntity = visitor;
                            //        entityClickedThisFrame = true;
                            //        break;
                            //    }
                            //}
                        }

                        if (clickedEntity != null)
                        {
                            if (_selectedEntity != null && _selectedEntity != clickedEntity)
                            {
                                _selectedEntity.IsSelected = false;
                            }
                            _selectedEntity = clickedEntity;
                            _selectedEntity.IsSelected = true;
                            _entityInfoPopupObject.GetComponent<EntityInfoPopupComponent>().Show(_selectedEntity);
                        }

                        else if (!entityClickedThisFrame && _entityInfoPopupObject.GetComponent<EntityInfoPopupComponent>().IsVisible && !popupHandledClick)
                        {
                            _entityInfoPopupObject.GetComponent<EntityInfoPopupComponent>().Hide();
                            if (_selectedEntity != null)
                            {
                                _selectedEntity.IsSelected = false;
                                _selectedEntity = null;
                            }
                        }
                    }

                }
            }
            if (_gameObjectsToRemove.Count > 0)
            {
                foreach (var gameObjectToRemove in _gameObjectsToRemove)
                {
                    gameObjects.Remove(gameObjectToRemove);
                    //visitors.Remove(visitorToRemove);
                    //Debug.WriteLine($"Visitor {gameObjectToRemove.VisitorId} has been despawned.");
                }
                _gameObjectsToRemove.Clear();
            }
            MouseState mouseState = Mouse.GetState();
            //shopButton.Update(mouseState, prevMouseState);

            // Shop ikon
            //if (!shopButton.IsClicked)
            //{
            //}
            //else
            //{
            //    // Shop vindue
            //    _shopWindow.IsVisible = !_shopWindow.IsVisible;

            //    _buildingsMenu.IsVisible = false;
            //    _habitatMenu.IsVisible = false;
            //    _animalMenu.IsVisible = false;
            //    _zookeeperMenu.IsVisible = false;
            //}

            //_shopWindow.Update(gameTime, mouseState, prevMouseState);
            //_buildingsMenu.Update(mouseState, prevMouseState);
            //_habitatMenu.Update(mouseState, prevMouseState);
            //_animalMenu.Update(mouseState, prevMouseState);
            //_zookeeperMenu.Update(mouseState, prevMouseState);

            //if (_infoButton.IsClicked)
            //{
            //    _showInfoPanel = !_showInfoPanel;
            //}

            prevMouseState = mouse;
            prevKeyboardState = keyboard;

            _previousPlacement = _currentPlacement;
            _wasPlacingRoadModeActive = _isPlacingRoadModeActive;

            base.Update(gameTime);
        }

        void HandleInput()
        {

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

            foreach (var road in RoadTiles)
            {
                Rectangle destinationRectangle = new Rectangle(
                        (int)road.x * TILE_SIZE,
                        (int)road.y * TILE_SIZE,
                        TILE_SIZE,
                        TILE_SIZE
                    );
                _spriteBatch.Draw(tileTextures[ROAD_TEXTURE_INDEX], destinationRectangle, Color.White);
            }

            foreach (var gameObject in gameObjects.Where(x => x.Layer == RenderLayer.World))
            {
                gameObject.Draw(_spriteBatch);
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


            _spriteBatch.End();

            // UI
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            foreach (var gameObject in gameObjects.Where(x => x.Layer == RenderLayer.Screen))
            {
                gameObject.Draw(_spriteBatch);
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

        public void Despawn(GameObject gameObject)
        {
            _gameObjectsToRemove.Add(gameObject);
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
            switch (type)
            {
                case "Buildings": _buildingsMenu.IsActive = true; break;
                case "Habitats": _habitatMenu.IsActive = true; break;
                case "Animals": _animalMenu.IsActive = true; break;
                case "Zookeepers": _zookeeperMenu.IsActive = true; break;
            }
        }
        public void StartHabitatPlacement(string size)
        {
            HideAllPlacementMenus();

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

        public void HideAllPlacementMenus()
        {
            _shopWindow.IsActive = false;
            _buildingsMenu.IsActive = false;
            _habitatMenu.IsActive = false;
            _animalMenu.IsActive = false;
            _zookeeperMenu.IsActive = false;
        }

        public void StartShopPlacement(string shopType)
        {
            HideAllPlacementMenus();

            if (shopType == "Shop - 1.000")
            {
                _currentPlacement = PlacementMode.PlaceVisitorShop;
                Debug.WriteLine("Placement mode: Visitor Shop activated");
            }
        }
        public void ToggleTilePlacementMode()
        {
            _isPlacingRoadModeActive = !_isPlacingRoadModeActive;
            //_buildingsMenu.IsVisible = false;
            //_habitatMenu.IsVisible = false;
            //_animalMenu.IsVisible = false;
            //_zookeeperMenu.IsVisible = false;
        }
        public void StartAnimalPlacement(string animalType)
        {
            HideAllPlacementMenus();

            var parts = animalType.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && Enum.TryParse<AnimalTypes>(parts[0], true, out var type) && int.TryParse(parts[1].Replace(".", ""), out var cost))
            {
                _currentPlacement = PlacementMode.PlaceAnimal;
                _animalTypeToPlace = type;
                _animalCostToPlace = cost;
                Debug.WriteLine($"Placement mode: {type} activated");
            }
            else
            {
                Debug.WriteLine($"Could not parse animal placement string: {animalType}");
            }
        }

        public void StartZookeeperPlacement(string name)
        {
            HideAllPlacementMenus();

            if (name == "Zookeeper - 5.000")
            {
                _currentPlacement = PlacementMode.PlaceZookeeper;
                Console.WriteLine("Placement mode: Zookeeper activated");
            }
        }
        public void StartTreePlacement()
        {
            HideAllPlacementMenus();
            _currentPlacement = PlacementMode.PlaceTree;
            Console.WriteLine("Placement mode: Tree activated");
        }

        public void StartWaterholePlacement()
        {
            HideAllPlacementMenus();
            _currentPlacement = PlacementMode.PlaceWaterhole;
            Console.WriteLine("Placement mode: Waterhole activated");
        }

        public GameObject Instantiate(GameObject gameObject)
        {
            gameObjects.Add(gameObject);
            gameObject.LoadContent(Content);
            return gameObject;
        }
    }
}
