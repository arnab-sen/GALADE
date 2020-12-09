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
    /// <para>Filters an input collection such that every element meets the given condition.</para>
    /// <para>Properties:</para>
    /// <para>Predicate&lt;T&gt; Condition: The condition to apply to each element T.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionInput: The input collection.</para>
    /// <para>2. IDataFlow&lt;IEnumerable&lt;T&gt;&gt; collectionOutput: The output collection. Will only be sent out if the operation is successful.</para>
    /// </summary>
    public class Where<T> : IDataFlow<IEnumerable<T>> // collectionInput
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public Predicate<T> Condition;
        
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

                if (Condition != null)
                {
                    outputCollection = inputCollection.Where(s => Condition(s));
                    if (collectionOutput != null) collectionOutput.Data = outputCollection;
                }

            }
        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public Where()
        {
            
        }
    }
}
