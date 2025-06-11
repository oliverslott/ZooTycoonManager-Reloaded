using Microsoft.Xna.Framework;
using System.Diagnostics;
using ZooTycoonManager.Components;
using ZooTycoonManager.Enums;

namespace ZooTycoonManager.Commands
{
    public class PlaceHabitatCommand : ICommand
    {
        private readonly Vector2 _position;
        private readonly HabitatSizeType _sizeType;
        private readonly decimal _cost;
        private GameObject _createdHabitat;

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

            _createdHabitat = EntityFactory.CreateHabitat(_position, _sizeType);
            //_createdHabitat = new Habitat(_position, _sizeType, GameWorld.Instance.GetNextHabitatId());
            //GameWorld.Instance.GetHabitats().Add(_createdHabitat);
            GameWorld.Instance.Instantiate(_createdHabitat);

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

            //if (_createdHabitat.GetAnimals().Count > 0)
            //{
            //    Debug.WriteLine($"Cannot undo {_sizeType} habitat placement: Habitat contains animals. Remove animals first.");
            //    return;
            //}

            foreach (var fence in _createdHabitat.GetComponent<HabitatComponent>().fences)
            {
                GameWorld.Instance.Despawn(fence);
            }

            GameWorld.Instance.Despawn(_createdHabitat);

            MoneyManager.Instance.AddMoney(_cost);
        }
    }
}