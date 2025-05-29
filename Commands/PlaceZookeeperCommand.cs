using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager.Commands
{
    public class PlaceZookeeperCommand : ICommand
    {
        private readonly Vector2 _position;
        private readonly decimal _cost;
        private Zookeeper _createdZookeeper;
        private Habitat _targetHabitat;

        public string Description => $"Place Zookeeper at ({_position.X:F0}, {_position.Y:F0})";

        public PlaceZookeeperCommand(Vector2 position, decimal cost = 500)
        {
            _position = position;
            _cost = cost;
        }

        public bool Execute()
        {
            // Find the habitat that contains the position
            _targetHabitat = GameWorld.Instance.GetHabitats().FirstOrDefault(h => h.ContainsPosition(_position));

            if (_targetHabitat == null)
            {
                Debug.WriteLine("Cannot place zookeeper: No habitat found at this position");
                return false;
            }

            Vector2 tilePos = GameWorld.PixelToTile(_position);

            // Check if the position is walkable and within bounds
            if (tilePos.X < 0 || tilePos.X >= GameWorld.GRID_WIDTH ||
                tilePos.Y < 0 || tilePos.Y >= GameWorld.GRID_HEIGHT ||
                !GameWorld.Instance.WalkableMap[(int)tilePos.X, (int)tilePos.Y])
            {
                Debug.WriteLine("Cannot place zookeeper: Position is not walkable");
                return false;
            }

            // Check if we have enough money
            if (!MoneyManager.Instance.SpendMoney(_cost))
            {
                Debug.WriteLine($"Not enough money to place zookeeper. Cost: ${_cost}, Available: ${MoneyManager.Instance.CurrentMoney}");
                return false;
            }

            // Create the zookeeper
            Vector2 spawnPos = GameWorld.TileToPixel(tilePos);
            _createdZookeeper = new Zookeeper(GameWorld.Instance.GetNextZookeeperId());
            _createdZookeeper.SetPosition(spawnPos);
            _createdZookeeper.LoadContent(GameWorld.Instance.Content);
            _createdZookeeper.SetHabitat(_targetHabitat);

            // Add to the habitat
            _targetHabitat.AddZookeeper(_createdZookeeper);

            Debug.WriteLine($"Placed zookeeper at {_position} for ${_cost}");
            return true;
        }

        public void Undo()
        {
            if (_createdZookeeper == null || _targetHabitat == null)
            {
                Debug.WriteLine("Cannot undo: No zookeeper was created or no target habitat");
                return;
            }

            // Remove the zookeeper from the habitat
            _targetHabitat.GetZookeepers().Remove(_createdZookeeper);

            // Refund the money
            MoneyManager.Instance.AddMoney(_cost);

            Debug.WriteLine($"Undid zookeeper placement at {_position}, refunded ${_cost}");
        }
    }
}
