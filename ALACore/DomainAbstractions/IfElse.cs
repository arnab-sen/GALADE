using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Fires an output event to one of two destinations based on whether the condition is true or false. The process begins either on an IEvent or when it receives an IDataFlow&lt;bool&gt;. The condition's value can also be set as a public property.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: Signals to start the process.</para>
    /// <para>2. IDataFlow&lt;bool&gt; condition: The condition to check. On receiving this value, the process will start.</para>
    /// <para>3. IEvent ifOutput: The IEvent destination if the condition is true.</para>
    /// <para>4. IEvent elseOutput: The IEvent destination if the condition is false.</para>
    /// </summary>
    public class IfElse : IEvent, IDataFlow<bool>
    {
        // Properties
        public string InstanceName = "Default";
        public bool Condition { set; get; } = true;

        // Ports
        private IEvent ifOutput;
        private IEvent elseOutput;

        public IfElse() { }

        public void ExecuteConditional()
        {
            if (Condition)
            {
                ifOutput?.Execute();
            }
            else
            {
                elseOutput?.Execute();
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            ExecuteConditional();
        }

        // IDataFlow<bool> implementation
        bool IDataFlow<bool>.Data
        {
            get => Condition;
            set
            {
                Condition = value;
                ExecuteConditional();
            }
        }
    }
}
