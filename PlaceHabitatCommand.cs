using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ZooTycoonManager
{
    /// <summary>
    /// Simple habitat command that just reverses the placement operation
    /// Much cleaner than tracking state!
    /// </summary>
    public class PlaceHabitatCommand : ICommand
    {
        private readonly Vector2 _position;
        private readonly decimal _cost;
        private Habitat _createdHabitat;
        
        public string Description => $"Place Habitat at ({_position.X:F0}, {_position.Y:F0})";
        
        public PlaceHabitatCommand(Vector2 position, decimal cost = 10000)
        {
            _position = position;
            _cost = cost;
        }
        
        public bool Execute()
        {
            // Check if we have enough money
            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"Not enough money to place habitat. Cost: ${_cost}, Available: ${MoneyManager.Instance.CurrentMoney}");
                return false;
            }
            
            // Create the habitat and let it place itself normally
            _createdHabitat = new Habitat(_position, Habitat.DEFAULT_ENCLOSURE_SIZE, Habitat.DEFAULT_ENCLOSURE_SIZE, 
                GameWorld.Instance.GetNextHabitatId());
            _createdHabitat.PlaceEnclosure(_position);
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
            if (_createdHabitat.GetAnimals().Count > 0)
            {
                Debug.WriteLine("Cannot undo habitat placement: Habitat contains animals. Remove animals first.");
                return;
            }
            
            // Simply remove the habitat and let it clean up after itself
            GameWorld.Instance.GetHabitats().Remove(_createdHabitat);
            _createdHabitat.RemoveEnclosure(); // New method that reverses PlaceEnclosure
            
            // Refund the money
            MoneyManager.Instance.AddMoney(_cost);
            
            Debug.WriteLine($"Undid habitat placement at {_position}, refunded ${_cost}");
        }
    }
} 