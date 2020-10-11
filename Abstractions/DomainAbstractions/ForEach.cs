using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Iterates through an IEnumerable, sends each element as an output, and signals when the iteration is complete.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionInput:</para>
    /// <para>2. IDataFlow&lt;T&gt; elementOutput:</para>
    /// <para>3. IEvent complete:</para>
    /// </summary>
    public class ForEach<T> : IDataFlow<IEnumerable<T>>
    {
        // Public fields and properties
        public string InstanceName = "Default";
        
        // Private fields
        private IEnumerable<T> collection;
        
        // Ports
        private IDataFlow<T> elementOutput;
        private IEvent complete;
        
        // IDataFlow<T1> implementation
        IEnumerable<T> IDataFlow<IEnumerable<T>>.Data
        {
            get => collection;
            set
            {
                collection = value;

                if (collection != null)
                {
                    foreach (var element in collection)
                    {
                        if (elementOutput != null) elementOutput.Data = element;
                    }

                    complete?.Execute();
                }
            }
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public ForEach()
        {
            
        }
    }
}
