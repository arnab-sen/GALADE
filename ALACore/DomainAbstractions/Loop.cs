using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Loops through a list by first sending out one element, and sending out the next element when it receives an event to do so.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent getNextValue: The event to indicate that the next element of the list should be sent out.</para>
    /// <para>2. IDataFlow&lt;List&lt;T&gt;&gt; inputList: The list to loop through. Should only be sent once per loop.</para>
    /// <para>3. IDataFlow&lt;T&gt; nextValue: The element of the list being sent out.</para>
    /// <para>4. IEvent loopComplete: Sends an output event to signal that the loop is complete. The pointer to the current element is reset to the beginning of the list.</para>
    /// </summary>
    public class Loop<T> : IEvent, IDataFlow<List<T>> // getNextValue, inputList
    {
        // Properties
        public string InstanceName { get; set; } = "Default";
        public bool ClearOnCompletion = true;

        // Private fields
        private List<T> list;
        private int currentIndex = -1;

        // Ports
        private IDataFlow<T> nextValue;
        private IEvent loopComplete;

        public Loop() { }

        public void SendNext()
        {
            currentIndex++;
            if (nextValue != null && currentIndex < list.Count) nextValue.Data = list.ElementAt(currentIndex);
            if (currentIndex >= list.Count)
            {
                if (ClearOnCompletion) list.Clear();
                currentIndex = -1;

                if (loopComplete != null) loopComplete.Execute();
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            SendNext();
        }

        // IDataFlow<IEnumerable> implementation

        List<T> IDataFlow<List<T>>.Data
        {
            get => list;
            set
            {
                list = value;
                SendNext();
            }
        }
    }
}
