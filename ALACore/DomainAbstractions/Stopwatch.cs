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
    /// A stopwatch that can be started and stopped (and reset) through an IEvent. The time in elapsed seconds will be output when this is stopped.
    /// </summary>
    public class Stopwatch : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string InstanceDescription { get; set; } = "";
        public int DecimalPlaces { get; set; } = 3;

        // Private fields
        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private IDataFlow<double> elapsedSeconds;

        // Ports

        // IEvent implementation
        void IEvent.Execute()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
            }
            else
            {
                _stopwatch.Stop();
                if (elapsedSeconds != null) elapsedSeconds.Data = Math.Round(_stopwatch.Elapsed.TotalSeconds, DecimalPlaces);

                _stopwatch.Reset();
            }
            
        }

        // Methods

        public Stopwatch()
        {

        }
    }
}
