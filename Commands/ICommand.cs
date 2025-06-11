namespace ZooTycoonManager.Commands
{
    public interface ICommand
    {
        bool Execute();
        void Undo();
        string Description { get; }
    }
}