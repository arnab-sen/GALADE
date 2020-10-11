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
    /// <para></para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;object&gt; senderOutput:</para>
    /// <para>2. IDataFlow&lt;object&gt; transitionOutput:</para>
    /// <para>3. IEvent eventHappened:</para>
    /// </summary>
    public class StateTransitionEvent<T> : IEventHandler where T : Enum
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public StateTransition<T> StateTransition { get; set; }
        public StateTransition<T>.StateTransitionedDelegate Lambda;
        public Func<Tuple<T, T>, StateTransition<T>, bool> Condition;

        // Private fields
        private object _sender;
        private T _oldState = default;
        private T _newState = default;

        // Ports
        private IDataFlow<object> senderOutput;
        private IDataFlow<Tuple<T, T>> transitionOutput;
        private IEvent eventHappened;
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public StateTransitionEvent(T oldState, T newState)
        {
            _oldState = oldState;
            _newState = newState;
        }

        // IEventHandler<T> implementation
        public object Sender
        {
            get => _sender;
            set
            {
                _sender = value;
                Subscribe("", _sender);
            }
        }

        public void Subscribe(string eventName, object sender)
        {
            if (StateTransition == null)
            {
                return;
            }

            try
            {
                // Subscribe the user-specified lambda
                if (Lambda != null) StateTransition.StateChanged += Lambda;

                // Propagate the sender, transition, and event (as an IEvent) after the event has been handled by the user-specified lambda
                StateTransition.StateChanged += (transition) =>
                {
                    if (StateTransition.Matches(_oldState, transition.Item1) && StateTransition.Matches(_newState, transition.Item2))
                    {
                        if (senderOutput != null) senderOutput.Data = sender;
                        if (transitionOutput != null) transitionOutput.Data = transition;
                        eventHappened?.Execute(); 
                    }
                };

            }
            catch (Exception e)
            {
                Logging.Log(e);
            }
        }
    }
}
