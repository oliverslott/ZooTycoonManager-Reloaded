namespace ZooTycoonManager
{
    /// <summary>
    /// Base interface for the Command pattern implementation
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns>True if the command was executed successfully, false otherwise</returns>
        bool Execute();
        
        /// <summary>
        /// Undo the command
        /// </summary>
        void Undo();
        
        /// <summary>
        /// Get a description of what this command does
        /// </summary>
        string Description { get; }
    }
} 