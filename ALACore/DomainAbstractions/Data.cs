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
    /// <para>Stores any data of any type, and outputs that data when it receives an IEvent call. Also supports limiting the output to a certain number of times.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: Starts the process of sending out the currently stored data.</para>
    /// <para>2. IDataFlow&lt;T&gt; inputData: The data to store.</para>
    /// <para>3. IDataFlowB&lt;T&gt; returnData: Returns the currently stored data.</para>
    /// <para>4. IDataFlowB&lt;T&gt; dataOutputB: A source to pull data from. This data replaces the currently stored data before the data output occurs.</para>
    /// <para>5. IDataFlow&lt;T&gt; dataOutput: The destination to send the currently stored data.</para>
    /// </summary>
    public class Data<T> : IEvent,  IDataFlow<T>, IDataFlowB<T> // start, inputData, returnData
    {
        // Properties
        public string InstanceName { get; set; } = "Default";
        public bool Perishable = false;
        public int PerishCount = 1; // Data cannot be pushed or pulled after it has been sent this amount of times
        public Func<T> Lambda;

        // Public fields
        public T StoredData = default(T);

        // Private fields
        private int numTimesSent = 0;

        // Ports
        private IDataFlowB<T> inputDataB;
        private IDataFlow<T> dataOutput;

        /// <summary>
        /// <para>Stores any data of any type, and outputs that data when it receives an IEvent call. Also supports limiting the output to a certain number of times.</para>
        /// </summary>
        public Data()
        {
            // Test();
        }

        private void Test()
        {
            
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            numTimesSent++;
            if (dataOutput != null)
            {
                // dataOutput.LogDataChange($"Data<{typeof(T)}> {InstanceName}", "dataOutput", , StoredData);
                if (inputDataB != null && inputDataB.Data != null) StoredData = inputDataB.Data;
                // StoredData.LogDataChange($"IDataFlow<{typeof(T)}>");
                if (Lambda != null) StoredData = Lambda();
                dataOutput.Data = StoredData;
            }
        }

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => StoredData;
            set
            {
                // StoredData.LogDataChange($"IDataFlow<{typeof(T)}>");
                StoredData = value;
            }
        }

        // IDataFlowB<T> implementation
        public event DataChangedDelegate DataChanged;
        T IDataFlowB<T>.Data
        {
            get
            {
                if (!(Perishable && numTimesSent >= PerishCount))
                {
                    numTimesSent++;
                    return StoredData;
                }
                else
                {
                    return default(T);
                }
            }
        }
    }
}
