using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ZooTycoonManager
{
    /// <summary>
    /// Command for placing a habitat that can be undone and redone
    /// </summary>
    public class PlaceHabitatCommand : ICommand
    {
        private readonly Vector2 _position;
        private readonly decimal _cost;
        private Habitat _createdHabitat;
        private readonly List<Vector2> _modifiedWalkableTiles;
        private readonly Dictionary<Vector2, bool> _originalWalkableStates;
        
        public string Description => $"Place Habitat at ({_position.X:F0}, {_position.Y:F0})";
        
        public PlaceHabitatCommand(Vector2 position, decimal cost = 10000)
        {
            _position = position;
            _cost = cost;
            _modifiedWalkableTiles = new List<Vector2>();
            _originalWalkableStates = new Dictionary<Vector2, bool>();
        }
        
        public bool Execute()
        {
            // Check if we have enough money
            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"Not enough money to place habitat. Cost: ${_cost}, Available: ${MoneyManager.Instance.CurrentMoney}");
                return false;
            }
            
            // Create the habitat
            _createdHabitat = new Habitat(_position, Habitat.DEFAULT_ENCLOSURE_SIZE, Habitat.DEFAULT_ENCLOSURE_SIZE, 
                GameWorld.Instance.GetNextHabitatId());
            
            // Store original walkable states before placing the enclosure
            StoreOriginalWalkableStates();
            
            // Place the enclosure (this modifies the walkable map)
            _createdHabitat.PlaceEnclosure(_position);
            
            // Add to the game world
            GameWorld.Instance.GetHabitats().Add(_createdHabitat);
            
            Debug.WriteLine($"Placed habitat at {_position} for ${_cost}");
            return true;
        }
        
        public void Undo()
        {
            if (_createdHabitat == null)
            {
                Debug.WriteLine("Cannot undo: No habitat was created");
                return;
            }
            
            // If there are animals in the habitat, we need to handle them
            // For now, we'll prevent undoing if there are animals
            if (_createdHabitat.GetAnimals().Count > 0)
            {
                Debug.WriteLine("Cannot undo habitat placement: Habitat contains animals. Remove animals first.");
                return; // Just return instead of throwing exception
            }
            
            // Remove the habitat from the game world
            GameWorld.Instance.GetHabitats().Remove(_createdHabitat);
            
            // Restore original walkable states
            RestoreOriginalWalkableStates();
            
            // Refund the money
            MoneyManager.Instance.AddMoney(_cost);
            
            Debug.WriteLine($"Undid habitat placement at {_position}, refunded ${_cost}");
        }
        
        private void StoreOriginalWalkableStates()
        {
            Vector2 centerTile = GameWorld.PixelToTile(_position);
            int radius = Habitat.GetEnclosureRadius();
            int startX = (int)centerTile.X - radius;
            int startY = (int)centerTile.Y - radius;
            int endX = (int)centerTile.X + radius;
            int endY = (int)centerTile.Y + radius;
            
            // Store states for all tiles that will be affected
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    if (x >= 0 && x < GameWorld.GRID_WIDTH && y >= 0 && y < GameWorld.GRID_HEIGHT)
                    {
                        Vector2 tilePos = new Vector2(x, y);
                        _originalWalkableStates[tilePos] = GameWorld.Instance.WalkableMap[x, y];
                        _modifiedWalkableTiles.Add(tilePos);
                    }
                }
            }
        }
        
        private void RestoreOriginalWalkableStates()
        {
            foreach (var tilePos in _modifiedWalkableTiles)
            {
                int x = (int)tilePos.X;
                int y = (int)tilePos.Y;
                
                if (x >= 0 && x < GameWorld.GRID_WIDTH && y >= 0 && y < GameWorld.GRID_HEIGHT)
                {
                    if (_originalWalkableStates.TryGetValue(tilePos, out bool originalState))
                    {
                        GameWorld.Instance.WalkableMap[x, y] = originalState;
                    }
                }
            }
        }
    }
} 