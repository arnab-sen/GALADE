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
    /// Executes a parameterless lambda when an IEvent is received. Sends an IEvent on completion.
    /// </summary>
    public class EventLambda : IEvent // start
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Action Lambda;

        // Private fields

        // Ports
        private IEvent complete;

        public EventLambda()
        {

        }

        // IEvent implementation
        void IEvent.Execute()
        {
            try
            {
                Lambda?.Invoke();
                complete?.Execute();
            }
            catch (Exception e)
            {

            }
        }

    }
}
