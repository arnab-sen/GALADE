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
    /// Casts an input instance of type T1 to an output instance of type T2. No output will be sent if the cast fails.
    /// </summary>
    public class Cast<T1, T2> : IDataFlow<T1> // input
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        T1 preCast;
        T2 postCast;

        // Ports
        private IDataFlow<T2> output;

        public Cast()
        {

        }

        // IDataFlow<T1> implementation
        T1 IDataFlow<T1>.Data
        {
            get => preCast;
            set
            {
                preCast = value;

                try
                {
                    postCast = (T2)((object)preCast);

                    if (output != null) output.Data = postCast;
                }
                catch (Exception e)
                {

                }
            }

        }
    }
}
