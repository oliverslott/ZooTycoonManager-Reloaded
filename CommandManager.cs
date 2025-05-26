using System.Collections.Generic;
using System.Diagnostics;

namespace ZooTycoonManager
{
    /// <summary>
    /// Manages command execution, undo, and redo operations
    /// </summary>
    public class CommandManager
    {
        private static CommandManager _instance;
        private static readonly object _lock = new object();
        
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        
        private const int MAX_UNDO_HISTORY = 50; // Limit undo history to prevent memory issues
        
        public static CommandManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CommandManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private CommandManager() { }
        
        /// <summary>
        /// Execute a command and add it to the undo stack
        /// </summary>
        /// <param name="command">The command to execute</param>
        public void ExecuteCommand(ICommand command)
        {
            try
            {
                bool success = command.Execute();
                
                // Only add to undo stack if the command executed successfully
                if (success)
                {
                    // Add to undo stack
                    _undoStack.Push(command);
                    
                    // Clear redo stack since we've executed a new command
                    _redoStack.Clear();
                    
                    // Limit undo history
                    if (_undoStack.Count > MAX_UNDO_HISTORY)
                    {
                        var tempStack = new Stack<ICommand>();
                        for (int i = 0; i < MAX_UNDO_HISTORY; i++)
                        {
                            tempStack.Push(_undoStack.Pop());
                        }
                        _undoStack.Clear();
                        while (tempStack.Count > 0)
                        {
                            _undoStack.Push(tempStack.Pop());
                        }
                    }
                    
                    Debug.WriteLine($"Executed command: {command.Description}");
                }
                else
                {
                    Debug.WriteLine($"Command failed to execute: {command.Description}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Exception while executing command: {command.Description}. Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Undo the last command
        /// </summary>
        /// <returns>True if undo was successful, false if no commands to undo</returns>
        public bool Undo()
        {
            if (_undoStack.Count == 0)
            {
                Debug.WriteLine("No commands to undo");
                return false;
            }
            
            try
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
                
                Debug.WriteLine($"Undid command: {command.Description}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Failed to undo command. Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Redo the last undone command
        /// </summary>
        /// <returns>True if redo was successful, false if no commands to redo</returns>
        public bool Redo()
        {
            if (_redoStack.Count == 0)
            {
                Debug.WriteLine("No commands to redo");
                return false;
            }
            
            try
            {
                var command = _redoStack.Pop();
                bool success = command.Execute();
                
                if (success)
                {
                    _undoStack.Push(command);
                    Debug.WriteLine($"Redid command: {command.Description}");
                    return true;
                }
                else
                {
                    // Put the command back on the redo stack if it failed
                    _redoStack.Push(command);
                    Debug.WriteLine($"Failed to redo command: {command.Description}");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Exception while redoing command. Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if undo is available
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;
        
        /// <summary>
        /// Check if redo is available
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;
        
        /// <summary>
        /// Get the description of the next command that would be undone
        /// </summary>
        public string GetUndoDescription()
        {
            return _undoStack.Count > 0 ? _undoStack.Peek().Description : "Nothing to undo";
        }
        
        /// <summary>
        /// Get the description of the next command that would be redone
        /// </summary>
        public string GetRedoDescription()
        {
            return _redoStack.Count > 0 ? _redoStack.Peek().Description : "Nothing to redo";
        }
        
        /// <summary>
        /// Clear all command history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            Debug.WriteLine("Command history cleared");
        }
    }
} 