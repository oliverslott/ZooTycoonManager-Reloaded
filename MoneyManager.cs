using System;
using System.Collections.Generic;

namespace ZooTycoonManager
{
    public class MoneyManager : ISubject
    {
        private List<IObserver> _observers = new List<IObserver>();
        private decimal _currentMoney;
        public decimal CurrentMoney => _currentMoney;

        public MoneyManager(decimal initialMoney)
        {
            _currentMoney = initialMoney;
        }

        public void AddMoney(decimal amount)
        {
            if (amount < 0)
            {
                // Or throw new ArgumentOutOfRangeException(nameof(amount), "Amount to add cannot be negative.");
                return; 
            }
            _currentMoney += amount;
            Notify();
        }

        public bool SpendMoney(decimal amount)
        {
            if (amount < 0)
            {
                // Or throw new ArgumentOutOfRangeException(nameof(amount), "Amount to spend cannot be negative.");
                return false;
            }

            if (_currentMoney >= amount)
            {
                _currentMoney -= amount;
                Notify();
                return true;
            }
            return false; // Not enough money
        }

        public void Attach(IObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (var observer in _observers)
            {
                observer.Update(_currentMoney);
            }
        }
    }
} 