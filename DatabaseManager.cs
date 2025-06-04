using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Microsoft.Xna.Framework.Content;

namespace ZooTycoonManager
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private static readonly object _lock = new object();
        private SqliteConnection _connection;

        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DatabaseManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private DatabaseManager()
        {
            _connection = new SqliteConnection("Data Source=mydb.db");
            _connection.Open();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            var createTablesCmd = _connection.CreateCommand();

            createTablesCmd.CommandText = @"CREATE TABLE IF NOT EXISTS Habitat (
                  habitat_id INTEGER PRIMARY KEY,
                  size INTEGER NOT NULL,
                  max_animals INTEGER NOT NULL,
                  name NVARCHAR(50) NOT NULL,
                  type NVARCHAR(50) NOT NULL,
                  position_x INTEGER NOT NULL,
                  position_y INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Shop (
                  shop_id INTEGER PRIMARY KEY,
                  type NVARCHAR(50) NOT NULL,
                  cost INTEGER NOT NULL,
                  position_x INTEGER NOT NULL,
                  position_y INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Visitor (
                    visitor_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    money INTEGER NOT NULL,
                    mood INTEGER NOT NULL,
                    hunger INTEGER NOT NULL,
                    habitat_id INTEGER,
                    shop_id INTEGER,
                    position_x INTEGER NOT NULL,
                    position_y INTEGER NOT NULL,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id),
                    FOREIGN KEY (shop_id) REFERENCES Shop(shop_id)
                );
                CREATE TABLE IF NOT EXISTS Zookeeper (
                    zookeeper_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    upkeep INTEGER NOT NULL,
                    habitat_id INTEGER,
                    position_x INTEGER NOT NULL,
                    position_y INTEGER NOT NULL,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id)
                );
                CREATE TABLE IF NOT EXISTS [Transaction] (
                    transaction_id INTEGER PRIMARY KEY,
                    price INTEGER NOT NULL,
                    datetime DATETIME NOT NULL,
                    visitor_id INTEGER NOT NULL,
                    shop_id INTEGER NOT NULL,
                    FOREIGN KEY (visitor_id) REFERENCES Visitor(visitor_id) ON DELETE CASCADE,
                    FOREIGN KEY (shop_id) REFERENCES Shop(shop_id) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS Species (
                    species_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Animal (
                    animal_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    mood INTEGER NOT NULL,
                    hunger INTEGER NOT NULL,
                    stress INTEGER NOT NULL,
                    habitat_id INTEGER NOT NULL,
                    position_x INTEGER NOT NULL,
                    position_y INTEGER NOT NULL,
                    species_id INTEGER NOT NULL,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id) ON DELETE RESTRICT,
                    FOREIGN KEY (species_id) REFERENCES Species(species_id)
                );
                CREATE TABLE IF NOT EXISTS VisitorFavoriteSpecies (
                    visitor_id INTEGER NOT NULL,
                    species_id INTEGER NOT NULL,
                    PRIMARY KEY (visitor_id, species_id),
                    FOREIGN KEY (visitor_id) REFERENCES Visitor(visitor_id) ON DELETE CASCADE,
                    FOREIGN KEY (species_id) REFERENCES Species(species_id) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS GameVariables (
                    key TEXT PRIMARY KEY,
                    value_numeric NUMERIC,
                    value_text TEXT
                );
                CREATE TABLE IF NOT EXISTS RoadTiles (
                    tile_x INTEGER NOT NULL,
                    tile_y INTEGER NOT NULL,
                    PRIMARY KEY (tile_x, tile_y)
                );";

            createTablesCmd.ExecuteNonQuery();

            var checkSpeciesCmd = _connection.CreateCommand();
            checkSpeciesCmd.CommandText = "SELECT COUNT(*) FROM Species";
            var speciesCount = Convert.ToInt32(checkSpeciesCmd.ExecuteScalar());

            if (speciesCount == 0)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    var insertSpeciesCmd = _connection.CreateCommand();
                    insertSpeciesCmd.Transaction = transaction;
                    
                    // Define all known species with IDs (assuming IDs start from 1)
                    var allSpeciesWithIds = new Dictionary<int, string>() 
                    {
                        {1, "Buffalo"}, {2, "Turtle"}, {3, "Chimpanze"}, {4, "Camel"}, {5, "Orangutan"},
                        {6, "Kangaroo"}, {7, "Wolf"}, {8, "Bear"}, {9, "Elephant"}, {10, "Polarbear"}, {11, "Goat"}
                    };

                    // Check existing species to avoid duplicates by name or id
                    var existingSpeciesNames = new HashSet<string>();
                    var existingSpeciesIds = new HashSet<int>();
                    var checkExistingCmd = _connection.CreateCommand();
                    checkExistingCmd.Transaction = transaction; 
                    checkExistingCmd.CommandText = "SELECT species_id, name FROM Species";
                    using (var reader = checkExistingCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingSpeciesIds.Add(reader.GetInt32(0));
                            existingSpeciesNames.Add(reader.GetString(1));
                        }
                    }

                    bool newSpeciesAdded = false;
                    foreach (var speciesEntry in allSpeciesWithIds)
                    {
                        // Insert if ID is not present AND name is not present to be safe
                        if (!existingSpeciesIds.Contains(speciesEntry.Key) && !existingSpeciesNames.Contains(speciesEntry.Value))
                        {
                            insertSpeciesCmd.CommandText = "INSERT INTO Species (species_id, name) VALUES (@id, @name);";
                            insertSpeciesCmd.Parameters.Clear(); 
                            insertSpeciesCmd.Parameters.AddWithValue("@id", speciesEntry.Key);
                            insertSpeciesCmd.Parameters.AddWithValue("@name", speciesEntry.Value);
                            insertSpeciesCmd.ExecuteNonQuery();
                            newSpeciesAdded = true;
                        }
                        // Optionally, handle cases where ID exists but name differs, or vice-versa (update or error)
                    }
                    
                    transaction.Commit();
                    if (newSpeciesAdded)
                    {
                        Debug.WriteLine("Populated/updated species in Species table.");
                    }
                    else
                    {
                        Debug.WriteLine("Species table already up-to-date.");
                    }
                }
            }

            // Insert default money if not present
            using (var transaction = _connection.BeginTransaction())
            {
                var insertDefaultMoneyCmd = _connection.CreateCommand();
                insertDefaultMoneyCmd.Transaction = transaction;
                insertDefaultMoneyCmd.CommandText = "INSERT OR IGNORE INTO GameVariables (key, value_numeric) VALUES ('CurrentMoney', 20000);";
                insertDefaultMoneyCmd.ExecuteNonQuery();

                // Add this to save the score
                var insertDefaultScoreCmd = _connection.CreateCommand();
                insertDefaultScoreCmd.CommandText = "INSERT OR IGNORE INTO GameVariables (key, value_numeric) VALUES ('CurrentScore', 60);"; // Default score 100
                insertDefaultScoreCmd.ExecuteNonQuery();

                transaction.Commit();
            }
        }

        private void SaveHabitats(SqliteTransaction transaction, List<Habitat> habitats)
        {
            foreach (Habitat habitatInstance in habitats)
            {
                Debug.WriteLine($"Attempting to save Habitat ID: {habitatInstance.HabitatId}, Name: {habitatInstance.Name}, Type: {habitatInstance.Type}");
                if (habitatInstance is ISaveable saveableObject)
                {
                    saveableObject.Save(transaction);
                }
                else
                {
                    Debug.WriteLine($"Warning: Habitat ID {habitatInstance.HabitatId} Name: {habitatInstance.Name} is not ISaveable and was not saved.");
                }
            }
        }

        private void SaveZookeepers(SqliteTransaction transaction, List<Habitat> habitats)
        {
            var allZookeepers = habitats.SelectMany(h => h.GetZookeepers()).ToList();
            foreach (Zookeeper zookeeperInstance in allZookeepers)
            {
                Debug.WriteLine($"Attempting to save Zookeeper ID: {zookeeperInstance.ZookeeperId}, Name: {zookeeperInstance.Name}");
                if (zookeeperInstance is ISaveable saveableObject)
                {
                    saveableObject.Save(transaction);
                }
                else
                {
                    Debug.WriteLine($"Warning: Zookeeper ID {zookeeperInstance.ZookeeperId} Name: {zookeeperInstance.Name} is not ISaveable and was not saved.");
                }
            }
        }

        private void SaveAnimals(SqliteTransaction transaction, List<Habitat> habitats)
        {
            foreach (Animal animalInstance in habitats.SelectMany(x => x.GetAnimals()))
            {
                Debug.WriteLine($"Attempting to save Animal ID: {animalInstance.AnimalId}, Name: {animalInstance.Name}, HabitatID: {animalInstance.HabitatId}");
                if (animalInstance is ISaveable saveableObject)
                {
                    saveableObject.Save(transaction);
                }
                else
                {
                    Debug.WriteLine($"Warning: Animal ID {animalInstance.AnimalId} Name: {animalInstance.Name} is not ISaveable and was not saved.");
                }
            }
        }

        private void SaveVisitors(SqliteTransaction transaction)
        {
            foreach (Visitor visitorInstance in GameWorld.Instance.GetVisitors())
            {
                Debug.WriteLine($"Attempting to save Visitor ID: {visitorInstance.VisitorId}, Name: {visitorInstance.Name}");
                if (visitorInstance is ISaveable saveableObject)
                {
                    saveableObject.Save(transaction);
                }
                else
                {
                    Debug.WriteLine($"Warning: Visitor ID {visitorInstance.VisitorId} Name: {visitorInstance.Name} is not ISaveable and was not saved.");
                }
            }
        }

        private void SaveShops(SqliteTransaction transaction, List<Shop> shops)
        {
            foreach (Shop shopInstance in shops)
            {
                Debug.WriteLine($"Attempting to save Shop ID: {shopInstance.ShopId}");
                if (shopInstance is ISaveable saveableObject)
                {
                    saveableObject.Save(transaction);
                }
                else
                {
                    Debug.WriteLine($"Warning: Shop ID {shopInstance.ShopId} is not ISaveable and was not saved.");
                }
            }
        }

        private void SaveCurrentMoney(SqliteTransaction transaction)
        {
            var saveMoneyCmd = _connection.CreateCommand();
            saveMoneyCmd.Transaction = transaction;
            saveMoneyCmd.CommandText = "UPDATE GameVariables SET value_numeric = @money WHERE key = 'CurrentMoney'";
            saveMoneyCmd.Parameters.AddWithValue("@money", MoneyManager.Instance.CurrentMoney);
            saveMoneyCmd.ExecuteNonQuery();
        }

        private void SaveScore(SqliteTransaction transaction)
        {
            var saveScoreCmd = _connection.CreateCommand();
            saveScoreCmd.Transaction = transaction;
            saveScoreCmd.CommandText = "UPDATE GameVariables SET value_numeric = @score WHERE key = 'CurrentScore'";
            saveScoreCmd.Parameters.AddWithValue("@score", ScoreManager.Instance.Score);
            saveScoreCmd.ExecuteNonQuery();
        }

        private void DeleteRemovedEntities(SqliteTransaction transaction, List<int> currentHabitatIds, List<int> currentAnimalIds, List<int> currentVisitorIds, List<int> currentShopIds, List<int> currentZookeeperIds)
        {
            var deleteCmd = _connection.CreateCommand();
            deleteCmd.Transaction = transaction;

            deleteCmd.CommandText = @"
                DELETE FROM [Transaction] 
                WHERE visitor_id NOT IN (SELECT visitor_id FROM Visitor)";
            deleteCmd.ExecuteNonQuery();

            deleteCmd.CommandText = @"
                DELETE FROM VisitorFavoriteSpecies 
                WHERE visitor_id NOT IN (SELECT visitor_id FROM Visitor)";
            deleteCmd.ExecuteNonQuery();

            if (currentAnimalIds.Any())
            {
                deleteCmd.CommandText = @"
                    DELETE FROM Animal 
                    WHERE animal_id NOT IN (" + string.Join(",", currentAnimalIds) + ")";
                deleteCmd.ExecuteNonQuery();
            }
            else
            {
                deleteCmd.CommandText = "DELETE FROM Animal";
                deleteCmd.ExecuteNonQuery();
            }

            if (currentVisitorIds.Any())
            {
                deleteCmd.CommandText = @"
                    DELETE FROM Visitor 
                    WHERE visitor_id NOT IN (" + string.Join(",", currentVisitorIds) + ")";
                deleteCmd.ExecuteNonQuery();
            }
            else
            {
                deleteCmd.CommandText = "DELETE FROM Visitor";
                deleteCmd.ExecuteNonQuery();
            }

            if (currentHabitatIds.Any())
            {
                deleteCmd.CommandText = @"
                    DELETE FROM Habitat 
                    WHERE habitat_id NOT IN (" + string.Join(",", currentHabitatIds) + ")";
                deleteCmd.ExecuteNonQuery();
            }
            else
            {
                deleteCmd.CommandText = "DELETE FROM Habitat";
                deleteCmd.ExecuteNonQuery();
            }

            // Delete removed shops
            if (currentShopIds.Any())
            {
                deleteCmd.CommandText = @"
                    DELETE FROM Shop 
                    WHERE shop_id NOT IN (" + string.Join(",", currentShopIds) + ")";
                deleteCmd.ExecuteNonQuery();
            }
            else
            {
                deleteCmd.CommandText = "DELETE FROM Shop";
                deleteCmd.ExecuteNonQuery();
            }

            // Delete removed zookeepers
            if (currentZookeeperIds.Any())
            {
                deleteCmd.CommandText = @"
                    DELETE FROM Zookeeper 
                    WHERE zookeeper_id NOT IN (" + string.Join(",", currentZookeeperIds) + ")";
                deleteCmd.ExecuteNonQuery();
            }
            else
            {
                deleteCmd.CommandText = "DELETE FROM Zookeeper";
                deleteCmd.ExecuteNonQuery();
            }
        }

        private void SaveRoadTiles(SqliteTransaction transaction)
        {
            var deleteCmd = _connection.CreateCommand();
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM RoadTiles";
            deleteCmd.ExecuteNonQuery();

            var saveRoadTileCmd = _connection.CreateCommand();
            saveRoadTileCmd.Transaction = transaction;
            saveRoadTileCmd.CommandText = "INSERT INTO RoadTiles (tile_x, tile_y) VALUES (@x, @y)";

            for (int x = 0; x < GameWorld.GRID_WIDTH; x++)
            {
                for (int y = 0; y < GameWorld.GRID_HEIGHT; y++)
                {
                    if (GameWorld.Instance.map.Tiles[x,y].Walkable && 
                        GameWorld.Instance.map.Tiles[x,y].TextureIndex == GameWorld.ROAD_TEXTURE_INDEX)
                    {
                        saveRoadTileCmd.Parameters.Clear();
                        saveRoadTileCmd.Parameters.AddWithValue("@x", x);
                        saveRoadTileCmd.Parameters.AddWithValue("@y", y);
                        saveRoadTileCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SaveGame()
        {
            Debug.WriteLine("Save Game button clicked. Saving game state...");
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    var habitats = GameWorld.Instance.GetHabitats();
                    var currentHabitatIds = habitats.Select(h => h.HabitatId).ToList();
                    var currentAnimalIds = habitats.SelectMany(h => h.GetAnimals()).Select(a => a.AnimalId).ToList();
                    var currentVisitorIds = GameWorld.Instance.GetVisitors().Select(v => v.VisitorId).ToList();
                    var currentShopIds = GameWorld.Instance.GetShops().Select(s => s.ShopId).ToList();
                    var currentZookeeperIds = habitats.SelectMany(h => h.GetZookeepers()).Select(zk => zk.ZookeeperId).ToList();

                    SaveHabitats(transaction, habitats);
                    SaveZookeepers(transaction, habitats);
                    SaveAnimals(transaction, habitats);
                    SaveVisitors(transaction);
                    SaveShops(transaction, GameWorld.Instance.GetShops());
                    SaveCurrentMoney(transaction);
                    SaveScore(transaction);
                    DeleteRemovedEntities(transaction, currentHabitatIds, currentAnimalIds, currentVisitorIds, currentShopIds, currentZookeeperIds);
                    SaveRoadTiles(transaction);

                    transaction.Commit();
                    Debug.WriteLine("Game state saved successfully.");
                }
                catch (SqliteException sqliteEx)
                {
                    Debug.WriteLine($"SQLite Error saving game state: {sqliteEx.Message}");
                    Debug.WriteLine($"SQLite Error Code: {sqliteEx.SqliteErrorCode}");
                    Debug.WriteLine($"SQLite Extended Error Code: {sqliteEx.SqliteExtendedErrorCode}");
                    transaction.Rollback();
                    Debug.WriteLine("Save transaction rolled back due to SQLite error.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Generic error saving game state: {ex.Message}");
                    Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    transaction.Rollback();
                    Debug.WriteLine("Save transaction rolled back due to generic error.");
                }
            }
        }

        private decimal LoadCurrentMoney()
        {
            decimal currentMoney = 20000;
            var moneyCommand = _connection.CreateCommand();
            moneyCommand.CommandText = "SELECT value_numeric FROM GameVariables WHERE key = 'CurrentMoney'";
            var moneyResult = moneyCommand.ExecuteScalar();
            if (moneyResult != null && moneyResult != DBNull.Value)
            {
                currentMoney = Convert.ToDecimal(moneyResult);
            }
            return currentMoney;
        }

        private int LoadScore()
        {
            int currentScore = 100; // Default score
            var scoreCommand = _connection.CreateCommand();
            scoreCommand.CommandText = "SELECT value_numeric FROM GameVariables WHERE key = 'CurrentScore'";
            var scoreResult = scoreCommand.ExecuteScalar();
            if (scoreResult != null && scoreResult != DBNull.Value)
            {
                currentScore = Convert.ToInt32(scoreResult);
            }
            return currentScore;
        }

        private List<Habitat> LoadHabitats(ContentManager content, out int nextHabitatId)
        {
            var loadedHabitats = new List<Habitat>();
            nextHabitatId = 1;
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT habitat_id, size, max_animals, name, type, position_x, position_y FROM Habitat";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var habitat = new Habitat();
                    habitat.Load(reader);
                    loadedHabitats.Add(habitat);

                    if (habitat.HabitatId >= nextHabitatId)
                    {
                        nextHabitatId = habitat.HabitatId + 1;
                    }
                }
            }
            return loadedHabitats;
        }

        private void LoadAnimals(ContentManager content, List<Habitat> loadedHabitats, out int nextAnimalId)
        {
            nextAnimalId = 1;
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT animal_id, name, mood, hunger, stress, habitat_id, position_x, position_y, species_id FROM Animal";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var animal = new Animal();
                    animal.Load(reader);
                    animal.LoadContent(content);

                    var habitat = loadedHabitats.FirstOrDefault(h => h.HabitatId == animal.HabitatId);
                    if (habitat != null)
                    {
                        animal.SetHabitat(habitat);
                        habitat.AddAnimal(animal);
                    }

                    if (animal.AnimalId >= nextAnimalId)
                    {
                        nextAnimalId = animal.AnimalId + 1;
                    }
                }
            }
        }

        private void LoadZookeepers(ContentManager content, List<Habitat> loadedHabitats, out int nextZookeeperId)
        {
            nextZookeeperId = 1;
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT zookeeper_id, name, upkeep, habitat_id, position_x, position_y FROM Zookeeper";
            
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var zookeeper = new Zookeeper(); // Parameterless constructor
                    zookeeper.Load(reader);          // Load basic properties
                    zookeeper.LoadContent(content);  // Load textures etc.

                    Habitat assignedHabitat = null;
                    if (zookeeper.AssignedHabitatId != -1)
                    {
                        assignedHabitat = loadedHabitats.FirstOrDefault(h => h.HabitatId == zookeeper.AssignedHabitatId);
                    }

                    if (assignedHabitat != null)
                    {
                        zookeeper.SetAssignedHabitat(assignedHabitat);
                        assignedHabitat.AddZookeeper(zookeeper); // Assumes Habitat has AddZookeeper
                        Debug.WriteLine($"Loaded Zookeeper ID {zookeeper.ZookeeperId} ({zookeeper.Name}) and assigned to Habitat ID {assignedHabitat.HabitatId}");
                    }
                    else if (zookeeper.AssignedHabitatId != -1)
                    {
                        Debug.WriteLine($"Warning: Zookeeper ID {zookeeper.ZookeeperId} ({zookeeper.Name}) refers to Habitat ID {zookeeper.AssignedHabitatId}, which was not found.");
                    }
                    else
                    {
                        Debug.WriteLine($"Warning: Zookeeper ID {zookeeper.ZookeeperId} ({zookeeper.Name}) is not assigned to any habitat.");
                        // Decide how to handle unassigned zookeepers if necessary, for now they are loaded but not in a habitat's list
                    }

                    zookeeper.InitializeBehavioralState(); // Initialize pathfinding, logic timers etc.
                    zookeeper.StartUpdateThread();       // Start its own update loop

                    if (zookeeper.ZookeeperId >= nextZookeeperId)
                    {
                        nextZookeeperId = zookeeper.ZookeeperId + 1;
                    }
                }
            }
            Debug.WriteLine($"Loaded zookeepers. Next Zookeeper ID will be {nextZookeeperId}");
        }

        private void LoadVisitors(ContentManager content, out int nextVisitorId)
        {
            nextVisitorId = 1;
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT visitor_id, name, money, mood, hunger, habitat_id, shop_id, position_x, position_y FROM Visitor";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var visitor = new Visitor(Vector2.Zero);
                    visitor.Load(reader);
                    visitor.LoadContent(content);
                    GameWorld.Instance.GetVisitors().Add(visitor);

                    if (visitor.VisitorId >= nextVisitorId)
                    {
                        nextVisitorId = visitor.VisitorId + 1;
                    }
                }
            }
        }

        private List<Shop> LoadShops(ContentManager content, out int nextShopId)
        {
            var loadedShops = new List<Shop>();
            nextShopId = 1;
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT shop_id, type, cost, position_x, position_y FROM Shop";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var shop = new Shop();
                    shop.Load(reader);
                    shop.LoadContent(content);
                    loadedShops.Add(shop);

                    if (shop.ShopId >= nextShopId)
                    {
                        nextShopId = shop.ShopId + 1;
                    }
                }
            }
            Debug.WriteLine($"Loaded {loadedShops.Count} shops. Next Shop ID will be {nextShopId}");
            return loadedShops;
        }

        private void LoadRoadTiles()
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT tile_x, tile_y FROM RoadTiles";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int tileX = reader.GetInt32(0);
                    int tileY = reader.GetInt32(1);
                    if (GameWorld.Instance != null && GameWorld.Instance.map != null)
                    {
                       GameWorld.Instance.UpdateTile(tileX, tileY, true, GameWorld.ROAD_TEXTURE_INDEX);
                    }
                    else
                    {
                        Debug.WriteLine($"Warning: Could not load road tile at ({tileX},{tileY}) because GameWorld or map is not initialized.");
                    }
                }
            }
        }

        public (List<Habitat> habitats, List<Shop> shops, int nextHabitatId, int nextAnimalId, int nextVisitorId, int nextShopId, int nextZookeeperId, decimal currentMoney, int currentScore) LoadGame(ContentManager content)
        {
            Debug.WriteLine("Loading game state...");
            List<Habitat> loadedHabitats;
            List<Shop> loadedShops;
            int nextHabitatId, nextAnimalId, nextVisitorId, nextShopId, nextZookeeperId;
            decimal currentMoney;
            int currentScore;

            try
            {
                currentMoney = LoadCurrentMoney();
                currentScore = LoadScore();
                loadedHabitats = LoadHabitats(content, out nextHabitatId);
                loadedShops = LoadShops(content, out nextShopId);
                LoadAnimals(content, loadedHabitats, out nextAnimalId);
                LoadZookeepers(content, loadedHabitats, out nextZookeeperId);
                LoadVisitors(content, out nextVisitorId);
                LoadRoadTiles();

                Debug.WriteLine("Game state loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading game state: {ex.Message}");
                loadedHabitats = new List<Habitat>();
                loadedShops = new List<Shop>();
                nextHabitatId = 1;
                nextAnimalId = 1;
                nextVisitorId = 1;
                nextShopId = 1;
                nextZookeeperId = 1;
                currentMoney = 20000;
                currentScore = 100; // Default score if loading fails
            }

            return (loadedHabitats, loadedShops, nextHabitatId, nextAnimalId, nextVisitorId, nextShopId, nextZookeeperId, currentMoney, currentScore);
        }

        public string GetSpeciesNameById(int speciesId)
        {
            string speciesName = "Unknown";
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT name FROM Species WHERE species_id = @id";
            command.Parameters.AddWithValue("@id", speciesId);

            try
            {
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    speciesName = Convert.ToString(result);
                }
                else
                {
                    Debug.WriteLine($"Warning: Species ID {speciesId} not found in Species table.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching species name for ID {speciesId}: {ex.Message}");
            }
            return speciesName;
        }

        public int GetSpeciesIdByName(string speciesName)
        {
            int speciesId = -1;
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT species_id FROM Species WHERE name = @name COLLATE NOCASE";
            command.Parameters.AddWithValue("@name", speciesName);

            try
            {
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    speciesId = Convert.ToInt32(result);
                }
                else
                {
                    Debug.WriteLine($"Warning: Species name '{speciesName}' not found in Species table.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching species ID for name '{speciesName}': {ex.Message}");
            }
            return speciesId;
        }
    }
} 