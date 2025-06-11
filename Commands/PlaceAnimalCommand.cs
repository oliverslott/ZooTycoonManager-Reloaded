using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Linq;
using ZooTycoonManager.Components;
using ZooTycoonManager.Enums;

namespace ZooTycoonManager.Commands
{
    public class PlaceAnimalCommand : ICommand
    {
        private readonly Vector2 _position;
        private readonly decimal _cost;
        private GameObject _createdAnimal;
        private GameObject _targetHabitat;
        private AnimalTypes _animalType;

        public string Description => $"Place Animal () at ({_position.X:F0}, {_position.Y:F0})";

        public PlaceAnimalCommand(Vector2 position, AnimalTypes animalType, decimal cost = 1000)
        {
            _position = position;
            _cost = cost;
            _animalType = animalType;
        }

        public bool Execute()
        {
            var test = GameWorld.Instance.GetHabitats();
            _targetHabitat = GameWorld.Instance.GetHabitats().FirstOrDefault(x => x.GetComponent<HabitatComponent>().ContainsPosition(_position));


            if (_targetHabitat == null)
            {
                Debug.WriteLine("Cannot place animal: No habitat found at this position");
                return false;
            }

            Vector2 tilePos = GameWorld.PixelToTile(_position);

            //if (tilePos.X < 0 || tilePos.X >= GameWorld.GRID_WIDTH || 
            //    tilePos.Y < 0 || tilePos.Y >= GameWorld.GRID_HEIGHT ||
            //    !GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y])
            //{
            //    Debug.WriteLine("Cannot place animal: Position is not walkable");
            //    return false;
            //}


            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"Not enough money to place animal. Cost: ${_cost}, Available: ${MoneyManager.Instance.CurrentMoney}");
                return false;
            }

            Vector2 spawnPos = GameWorld.TileToPixel(tilePos);
            _createdAnimal = EntityFactory.CreateAnimal(spawnPos, _animalType, _targetHabitat);
            //_createdAnimal = new Animal(GameWorld.Instance.GetNextAnimalId(), _speciesId);
            //_createdAnimal.SetPosition(spawnPos);
            //_createdAnimal.LoadContent();
            //_createdAnimal.SetHabitat(_targetHabitat);

            //_targetHabitat.AddAnimal(_createdAnimal);

            GameWorld.Instance.Instantiate(_createdAnimal);

            Debug.WriteLine($"Placed animal at {_position} for ${_cost}");
            return true;
        }

        public void Undo()
        {
            if (_createdAnimal == null || _targetHabitat == null)
            {
                Debug.WriteLine("Cannot undo: No animal was created or no target habitat");
                return;
            }

            //_targetHabitat.GetAnimals().Remove(_createdAnimal);
            GameWorld.Instance.Despawn(_createdAnimal);

            MoneyManager.Instance.AddMoney(_cost);

            Debug.WriteLine($"Undid animal placement at {_position}, refunded ${_cost}");
        }
    }
}