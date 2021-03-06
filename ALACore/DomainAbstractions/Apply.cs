﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Applies a lambda on an input of type T1 and returns an output of type T2.</para>
    /// <para>Ports:</para>
    /// <para>IDataFlow&lt;T1&gt; input: The input to the lambda.</para>
    /// <para>IDataFlow&lt;T2&gt; output: The output from the lambda.</para>
    /// <para>IDataFlowB&lt;Func&lt;T1,T2&gt;&gt; lambdaInput: A lambda can be pulled from an external source through this port.</para>
    /// </summary>
    public class Apply<T1, T2> : IDataFlow<T1> // input
    {
        // Properties
        public string InstanceName { get; set; } = "Default";
        public Func<T1, T2> Lambda;

        // Private fields
        private T1 lastInput = default;
        private T2 storedValue;

        // Ports
        private IDataFlowB<Func<T1, T2>> lambdaInput;
        private IDataFlow<T2> output;

        /// <summary>
        /// <para>Applies a lambda on an input of type T1 and returns an output of type T2.</para>
        /// </summary>
        public Apply() { }

        // IDataFlow<T1> implementation
        T1 IDataFlow<T1>.Data
        {
            get => lastInput;
            set
            {
                try
                {
                    lastInput = value;
                    
                    // Pulling the Lambda from an external source at runtime has priority over setting the Lambda through the public property.
                    // A default Lambda can thus be set through the public setter, and will be overwritten if a new value is available from the external source.
                    if (lambdaInput != null && lambdaInput.Data != null) Lambda = lambdaInput.Data; 

                    storedValue = Lambda(lastInput);
                }
                catch (Exception e)
                {
                    Libraries.Logging.Log(e);
                }

                if (output != null && storedValue != null) output.Data = storedValue;
            } 
        }

    }
}