namespace ZooTycoonManager
{
    public interface IInspectableEntity
    {
        int Id { get; }
        string Name { get; }
        int Mood { get; }
        int Hunger { get; }
        bool IsSelected { get; set; }
        // Potentially add other common properties or methods if needed
    }
} 