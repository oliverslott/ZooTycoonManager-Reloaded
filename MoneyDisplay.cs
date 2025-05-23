using System.Globalization;

namespace ZooTycoonManager
{
    public class MoneyDisplay : IObserver
    {
        public string MoneyText { get; private set; }

        public MoneyDisplay()
        {
            MoneyText = string.Empty; // Initialize with an empty string or a default value
        }

        public void Update(decimal newMoneyAmount)
        {
            // Format the money amount as currency. Example: $10,000.00
            // Using CultureInfo.CurrentCulture to respect local currency formats if needed,
            // or specify a fixed one like new CultureInfo("en-US") for consistency.
            MoneyText = string.Format(CultureInfo.CurrentCulture, "Money: {0:C}", newMoneyAmount);
        }
    }
} 