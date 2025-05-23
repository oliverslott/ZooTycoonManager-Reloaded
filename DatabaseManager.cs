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
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id) ON DELETE RESTRICT
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
                    insertSpeciesCmd.CommandText = @"
                        INSERT INTO Species (name) VALUES ('Goat');
                    ";
                    insertSpeciesCmd.ExecuteNonQuery();
                    transaction.Commit();
                    Debug.WriteLine("Populated default species into Species table.");
                }
            }

            // Insert default money if not present
            using (var transaction = _connection.BeginTransaction())
            {
                var insertDefaultMoneyCmd = _connection.CreateCommand();
                insertDefaultMoneyCmd.Transaction = transaction;
                insertDefaultMoneyCmd.CommandText = "INSERT OR IGNORE INTO GameVariables (key, value_numeric) VALUES ('CurrentMoney', 20000);";
                insertDefaultMoneyCmd.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        public void SaveGame(List<Habitat> habitats)
        {
            Debug.WriteLine("Save Game button clicked. Saving game state...");
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    // Get all current IDs from the game state
                    var currentHabitatIds = habitats.Select(h => h.HabitatId).ToList();
                    var currentAnimalIds = habitats.SelectMany(h => h.GetAnimals()).Select(a => a.AnimalId).ToList();
                    var currentVisitorIds = GameWorld.Instance.GetVisitors().Select(v => v.VisitorId).ToList();

                    // First save all current game state
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

                    // Save visitors
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

                    // Save current money
                    var saveMoneyCmd = _connection.CreateCommand();
                    saveMoneyCmd.Transaction = transaction;
                    saveMoneyCmd.CommandText = "UPDATE GameVariables SET value_numeric = @money WHERE key = 'CurrentMoney'";
                    saveMoneyCmd.Parameters.AddWithValue("@money", MoneyManager.Instance.CurrentMoney);
                    saveMoneyCmd.ExecuteNonQuery();

                    // Now delete objects that no longer exist in the game state
                    var deleteCmd = _connection.CreateCommand();
                    deleteCmd.Transaction = transaction;

                    // Delete transactions for removed visitors
                    deleteCmd.CommandText = @"
                        DELETE FROM [Transaction] 
                        WHERE visitor_id NOT IN (SELECT visitor_id FROM Visitor)";
                    deleteCmd.ExecuteNonQuery();

                    // Delete visitor favorite species for removed visitors
                    deleteCmd.CommandText = @"
                        DELETE FROM VisitorFavoriteSpecies 
                        WHERE visitor_id NOT IN (SELECT visitor_id FROM Visitor)";
                    deleteCmd.ExecuteNonQuery();

                    // Delete animals that no longer exist
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

                    // Delete visitors that no longer exist
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

                    // Delete habitats that no longer exist
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

        public (List<Habitat> habitats, int nextHabitatId, int nextAnimalId, int nextVisitorId, decimal currentMoney) LoadGame(ContentManager content)
        {
            Debug.WriteLine("Loading game state...");
            var loadedHabitats = new List<Habitat>();
            int nextHabitatId = 1;
            int nextAnimalId = 1;
            int nextVisitorId = 1;
            decimal currentMoney = 20000; // Default starting money

            try
            {
                // Load current money
                var moneyCommand = _connection.CreateCommand();
                moneyCommand.CommandText = "SELECT value_numeric FROM GameVariables WHERE key = 'CurrentMoney'";
                var moneyResult = moneyCommand.ExecuteScalar();
                if (moneyResult != null && moneyResult != DBNull.Value)
                {
                    currentMoney = Convert.ToDecimal(moneyResult);
                }

                // Load habitats
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT habitat_id, size, max_animals, name, type, position_x, position_y FROM Habitat";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var habitat = new Habitat(Vector2.Zero, Habitat.DEFAULT_ENCLOSURE_SIZE, Habitat.DEFAULT_ENCLOSURE_SIZE);
                        habitat.Load(reader);
                        loadedHabitats.Add(habitat);

                        if (habitat.HabitatId >= nextHabitatId)
                        {
                            nextHabitatId = habitat.HabitatId + 1;
                        }
                    }
                }

                // Load animals
                command.CommandText = "SELECT animal_id, name, mood, hunger, stress, habitat_id, position_x, position_y FROM Animal";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var animal = new Animal();
                        animal.Load(reader);
                        animal.LoadContent(content);

                        // Find the habitat and add the animal to it
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

                // Load visitors
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

                Debug.WriteLine("Game state loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading game state: {ex.Message}");
            }

            return (loadedHabitats, nextHabitatId, nextAnimalId, nextVisitorId, currentMoney);
        }
    }
} 