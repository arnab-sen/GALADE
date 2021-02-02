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
    /// <para>Uses a lambda to extract a collection out of each element, and then combines all of the collections into one collection.</para>
    /// <para>Properties:</para>
    /// <para>Func&lt;T, IEnumerable&lt;T&gt;&gt; Lambda: The lambda to extract an IEnumerable&lt;T&gt; from each element T.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionInput: The input collection.</para>
    /// <para>2. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionOutput: The output collection. Will only be sent out if the operation is successful.</para>
    /// </summary>
    public class SelectMany<T> : IDataFlow<IEnumerable<T>> // collectionInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Func<T, IEnumerable<T>> Lambda;
        
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
                    outputCollection = inputCollection.SelectMany(s => Lambda(s));
                    if (collectionOutput != null) collectionOutput.Data = outputCollection;
                }

            }
        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public SelectMany()
        {
            
        }
    }
}