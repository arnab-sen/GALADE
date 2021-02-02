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
    /// <para>Uses a lambda to extract an element out of each element, and then combines all of the elements into one collection.</para>
    /// <para>Properties:</para>
    /// <para>Func&lt;T, T&gt; Lambda: The lambda to extract an element T from each element T.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionInput: The input collection.</para>
    /// <para>2. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionOutput: The output collection. Will only be sent out if the operation is successful.</para>
    /// </summary>
    public class Select<T> : IDataFlow<IEnumerable<T>> // collectionInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Func<T, T> Lambda;
        
        // Private fields
        private IEnumerable<T> inputCollection;
        private IEnumerable<T> outputCollection;
        
        // Ports
        private IDataFlow<IEnumerable<T>> collectionOutput;
        
        // IDataFlow<IEnumerable<T>> implementation
        IEnumerable<T> IDataFlow<IEnumerable<T>>.Data
        {
            get => inputCollection;
            set
            {
                inputCollection = value;

                if (Lambda != null)
                {
                    outputCollection = inputCollection.Select(s => Lambda(s));
                    if (collectionOutput != null) collectionOutput.Data = outputCollection;
                }

            }
        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public Select()
        {
            
        }
    }
}