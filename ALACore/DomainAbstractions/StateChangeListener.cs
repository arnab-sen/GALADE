using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Propagates the StateTransition.StateChanged event as an IEvent.
    /// This propagation can be controlled by specifying that a state, or a combination of flag states, should match the current state using CurrentStateShouldMatch.</para>
    /// <para>For example: <code>CurrentStateShouldMatch = State1</code><code>CurrentStateShouldMatch = State1 | State2 | State3</code></para>
    /// <para>Properties:</para>
    /// <para>1. Enums.DiagramMode CurrentStateShouldMatch: Specific states to check. If All, the IEvent will be sent regardless of the state.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent getState: Asks the StateTransition for the current state.</para>
    /// <para>2. IEvent stateChanged: The StateTransition.StateChanged event as an IEvent.</para>
    /// <para>2. IDataFlow&lt;Enums.DiagramMode&gt; currentStateOutput: The StateTransition.CurrentState.</para>
    /// <para>2. IDataFlow&lt;string&gt; currentStateAsStringOutput: The string representation of the StateTransition.CurrentState.</para>
    /// </summary>
    public class StateChangeListener : IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public Enums.DiagramMode CurrentStateShouldMatch { get; set; } = Enums.DiagramMode.All;

        // Private fields


        // Ports
        private IEvent stateChanged;
        private IDataFlow<Enums.DiagramMode> currentStateOutput;
        private IDataFlow<string> currentStateAsStringOutput;
        private IDataFlow<Tuple<Enums.DiagramMode, Enums.DiagramMode>> transitionOutput;

        // IEvent implementation
        void IEvent.Execute()
        {
            if (currentStateOutput != null) currentStateOutput.Data = StateTransition.CurrentState;
            if (currentStateAsStringOutput != null) currentStateAsStringOutput.Data = Enum.GetName(typeof(Enums.DiagramMode), StateTransition.CurrentState);
        }

        // PostWiringInitialize
        private void PostWiringInitialize()
        {
            if (StateTransition != null)
            {
                StateTransition.StateChanged += transition =>
                    {
                        if (CurrentStateShouldMatch == Enums.DiagramMode.All || StateTransition.CurrentStateMatches(CurrentStateShouldMatch))
                        {
                            stateChanged?.Execute();
                            if (currentStateOutput != null) currentStateOutput.Data = StateTransition.CurrentState;
                            if (currentStateAsStringOutput != null) currentStateAsStringOutput.Data = Enum.GetName(typeof(Enums.DiagramMode), StateTransition.CurrentState);
                            if (transitionOutput != null) transitionOutput.Data = transition;
                        }
                    }; 
            }
        }

        /// <summary>
        /// <para>Propagates the StateTransition.StateChanged event as an IEvent</para>
        /// </summary>
        public StateChangeListener()
        {
            
        }
    }
}
