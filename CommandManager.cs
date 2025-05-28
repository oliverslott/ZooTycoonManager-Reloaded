using System.Collections.Generic;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class CommandManager
    {
        private static CommandManager _instance;
        private static readonly object _lock = new object();
        
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        
        private const int MAX_UNDO_HISTORY = 50;
        
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
        public void ExecuteCommand(ICommand command)
        {
            try
            {
                bool success = command.Execute();
                
                if (success)
                {
                    _undoStack.Push(command);
                    
                    _redoStack.Clear();
                    

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

        public bool CanUndo => _undoStack.Count > 0;
        
        public bool CanRedo => _redoStack.Count > 0;

        public string GetUndoDescription()
        {
            return _undoStack.Count > 0 ? _undoStack.Peek().Description : "Nothing to undo";
        }

        public string GetRedoDescription()
        {
            return _redoStack.Count > 0 ? _redoStack.Peek().Description : "Nothing to redo";
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            Debug.WriteLine("Command history cleared");
        }
    }
} 