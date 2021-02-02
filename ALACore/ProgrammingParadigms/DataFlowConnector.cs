using System.Collections.Generic;
using System.Linq;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// A dataflow with a single scalar value with a primitive data type and a "OnChanged' event.
    /// OR think of it as an event with data, the receivers are able to read the data at any time.
    /// Or think of it as an implementation of a global variable and an observer pattern, with access to the variable and observer pattern restricted to the line connections on the diagram.
    /// Unidirectional - every line is one direction implying sender(s) and receiver(s).
    /// You can have multiple senders and receivers.
    /// The data is stored in the wire so receivers that don't act on the event can read its value at any time. Receivers cant change the data or send the event. 
    /// </summary>
    /// <typeparam name="T">Generic data type</typeparam>
    public interface IDataFlow<T>
    {
        T Data { get; set; }
    }

    public delegate void DataChangedDelegate();

    /// <summary>
    /// A reversed IDataFlow, the IDataFlow pushes data to the destination whereas IDataFlowB pulls data from source.
    /// However, the DataChanged event will notify the destination when change happens.
    /// </summary>
    /// <typeparam name="T">Generic data type</typeparam>
    public interface IDataFlowB<T>
    {
        T Data { get; }
        event DataChangedDelegate DataChanged;
    }

    /// <summary>
    /// It fans out data flows by creating a list and assign the data to the element in the list.
    /// Moreover, any IDataFlow and IDataFlowB can be transferred bidirectionally.
    /// </summary>
    /// <typeparam name="T">Generic data type</typeparam>
    public class DataFlowConnector<T> : IDataFlow<T>, IDataFlowB<T> // input, returnDataB
    {
        // Properties
        public string InstanceName { get; set; } = "Default";
        public T Data;

        // Private fields

        // Ports
        private List<IDataFlow<T>> fanoutList = new List<IDataFlow<T>>();

        /// <summary>
        /// Fans out a data flow to multiple data flows, or connect IDataFlow and IDataFlowB
        /// </summary>
        public DataFlowConnector() { }

        public override string ToString()
        {
            return $"DataFlowConnector<{typeof(T).ToString().Split('.').Last()}> {InstanceName}";
        }

        // IDataFlow<T> implementation ---------------------------------
        T IDataFlow<T>.Data
        {
            get => Data;
            set
            {
                Data = value;
                foreach (var f in fanoutList) f.Data = value;
                DataChanged?.Invoke();
            }
        }

        // IDataFlowB<T> implementation ---------------------------------
        public event DataChangedDelegate DataChanged;
        T IDataFlowB<T>.Data { get => Data; }
    }
}
