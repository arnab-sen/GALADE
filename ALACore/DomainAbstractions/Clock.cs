using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>A clock that sends intermittent event pulses.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start: Starts the clock.</para>
    /// <para>2. IEvent start: Stops the clock.</para>
    /// <para>3. IEvent pulse: Fires an event at the start of each period.</para>
    /// </summary>
    public class Clock : IEvent // start
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public int Period { get; set; } = 1000;
        public double NumIterations { get; set; } = double.PositiveInfinity;
        public bool SendInitialPulse { get; set; } = true;

        // Private fields
        private bool _keepRunning = true;
        private bool _isRunning = false;

        // Ports
        private IEventB stop;
        private IEvent eventHappened;

        private async Task Run()
        {
            if (SendInitialPulse) eventHappened?.Execute();

            if (NumIterations.Equals(double.PositiveInfinity))
            {
                while (_keepRunning)
                {
                    await Task.Delay(Period);
                    eventHappened?.Execute();
                }

                _isRunning = false;
            }
            else
            {
                for (int i = 0; i < NumIterations; i++)
                {
                    if (!_keepRunning)
                    {
                        _isRunning = false;
                        break;
                    }

                    await Task.Delay(Period);
                    eventHappened?.Execute();
                }

                _isRunning = false;
            }

        }

        // IEvent implementation
        void IEvent.Execute()
        {
            if (!_isRunning)
            {
                _keepRunning = true;
                _isRunning = true;
                var _fireAndForget = Run(); 
            }
        }

        private void PostWiringInitialize()
        {
            if (stop != null)
            {
                stop.EventHappened += () =>
                {
                    _keepRunning = false;
                };
            }
        }

        public Clock()
        {

        }
    }
}
