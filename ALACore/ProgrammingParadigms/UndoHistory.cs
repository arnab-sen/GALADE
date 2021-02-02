using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// Stores state information in a timeline. Supports both undo and redo.
    /// A (usually singleton) instance of this class acts as the Caretaker in the Memento design pattern, with any class
    /// implementing IMemento being an Originator.
    /// </summary>
    public class UndoHistory
    {
        // Public strings and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Stack<Tuple<IMemento, string>> undoStack = new Stack<Tuple<IMemento, string>>();
        private Stack<Tuple<IMemento, string>> redoStack = new Stack<Tuple<IMemento, string>>();

        public void Push(IMemento originator)
        {
            undoStack.Push(Tuple.Create(originator, originator.Memento));
            originator.SaveMemento();
            redoStack.Clear(); // Branching from timeline, so erase the current redo cache
        }

        public void Undo()
        {
            MementoStackShift(undoStack, redoStack);
        }

        public void Redo()
        {
            MementoStackShift(redoStack, undoStack);
        }

        private void MementoStackShift(Stack<Tuple<IMemento, string>> sourceStack, Stack<Tuple<IMemento, string>> destinationStack)
        {
            if (sourceStack.Count > 0)
            {
                var latest = sourceStack.Pop();
                latest.Item1.LoadMemento(latest.Item2);

                destinationStack.Push(Tuple.Create(latest.Item1, latest.Item1.Memento));
                latest.Item1.SaveMemento();
            }
        }
    }
}
