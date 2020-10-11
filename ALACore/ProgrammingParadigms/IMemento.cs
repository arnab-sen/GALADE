using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    public delegate void ActionPerformedDelegate(IMemento source);

    /// <summary>
    /// A facet of the Memento design pattern.
    /// The Originator (the IMemento implementer) is in charge of defining how mementos should be made and read from.
    /// It sends itself as an IMemento to the Caretaker, which handles calling SaveMemento() and LoadMemento().
    /// The Originator should not call either of the functions, and should instead leave that to the Caretaker. The Originator should, however, invoke
    /// ActionPerformed whenever an undoable action is performed, to alert whatever is in charge of sending the IMemento to the Caretaker.
    /// </summary>
    /// <param name="source"></param>
    public interface IMemento
    {
        string Memento { get; }
        void SaveMemento();
        void LoadMemento(string memento);
        event ActionPerformedDelegate ActionPerformed;
    }
}
