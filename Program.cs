using System;
using System.Threading;


namespace ZooTycoonManager
{
    public static class Program
    {
        public static Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            mutex = new Mutex(true, "GameMutex", out createdNew);

            if (!createdNew)
            {
                Console.WriteLine("Already running");
                return;
            }

            GameWorld.Instance.Run();
            
        }
    }
}
