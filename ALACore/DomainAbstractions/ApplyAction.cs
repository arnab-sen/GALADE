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
    /// <para>Applies a lambda of return type void on an input of type T.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;T&gt; input: The input to the lambda.</para>
    /// </summary>
    public class ApplyAction<T> : IDataFlow<T> // input
    {
        // Properties
        public string InstanceName = "Default";
        public Action<T> Lambda;

        // Private fields
        private T lastInput = default;

        // Ports

        /// <summary>
        /// <para>Applies a lambda of return type void on an input of type T.</para>
        /// </summary>
        public ApplyAction() { }

        private void TestLambda(T input)
        {
        }

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => lastInput;
            set
            {
                try
                {
                    if (InstanceName == "") TestLambda(value);

                    lastInput = value;
                    
                    Lambda(lastInput);
                }
                catch (Exception e)
                {
                    Libraries.Logging.Log(e);
                }
            } 
        }

    }
}