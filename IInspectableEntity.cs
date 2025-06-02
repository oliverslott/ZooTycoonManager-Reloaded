namespace ZooTycoonManager
{
    public interface IInspectableEntity
    {
        int Id { get; }
        string Name { get; }
        int Mood { get; }
        int Hunger { get; }
        int SpeciesId { get; }
        bool IsSelected { get; set; }
    }
} 