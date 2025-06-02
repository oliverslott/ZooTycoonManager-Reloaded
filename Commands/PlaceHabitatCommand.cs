using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ZooTycoonManager.Commands
{
    public class PlaceHabitatCommand : ICommand
    {
        private readonly Vector2 _position;
        private readonly HabitatSizeType _sizeType;
        private readonly decimal _cost;
        private Habitat _createdHabitat;
        
        public string Description => $"Place {_sizeType} Habitat at ({_position.X:F0}, {_position.Y:F0})";
        
        public PlaceHabitatCommand(Vector2 position, HabitatSizeType sizeType, decimal cost)
        {
            _position = position;
            _sizeType = sizeType;
            _cost = cost;
        }
        
        public bool Execute()
        {
            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"Not enough money to place {_sizeType} habitat. Cost: ${_cost}, Available: ${MoneyManager.Instance.CurrentMoney}");
                return false;
            }
            
            _createdHabitat = new Habitat(_position, _sizeType, GameWorld.Instance.GetNextHabitatId());
            GameWorld.Instance.GetHabitats().Add(_createdHabitat);
            
            Debug.WriteLine($"Placed {_sizeType} habitat at {_position} for ${_cost}");
            return true;
        }
        
        public void Undo()
        {
            if (_createdHabitat == null)
            {
                Debug.WriteLine("Cannot undo: No habitat was created");
                return;
            }
            
            if (_createdHabitat.GetAnimals().Count > 0)
            {
                Debug.WriteLine($"Cannot undo {_sizeType} habitat placement: Habitat contains animals. Remove animals first.");
                return;
            }
            
            GameWorld.Instance.GetHabitats().Remove(_createdHabitat);
            _createdHabitat.RemoveEnclosure(); 
            
            MoneyManager.Instance.AddMoney(_cost);
            
            Debug.WriteLine($"Undid {_sizeType} habitat placement at {_position}, refunded ${_cost}");
        }
    }
} 