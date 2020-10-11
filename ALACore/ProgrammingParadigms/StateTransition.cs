using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    public class StateTransition<T> where T : Enum
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public T CurrentState { get; private set; }
        public Tuple<T, T> LatestTransition { get; private set; }
        public Func<T, T, bool> Matches { get; set; }
        public delegate void StateTransitionedDelegate(Tuple<T, T> transition);
        public event StateTransitionedDelegate StateChanged;

        // Private fields

        public void Update(T newState)
        {
            if (!newState.Equals(CurrentState))
            {
                LatestTransition = Tuple.Create(CurrentState, newState);
                CurrentState = newState;

                StateChanged?.Invoke(LatestTransition);
                Logging.Log($"StateTransition from {LatestTransition.Item1} to {LatestTransition.Item2}");
            }
        }

        public bool CurrentStateMatches(T flag)
        {
            return Matches?.Invoke(flag, CurrentState) ?? false;
            // return (CurrentState & flag) != 0;
        }

        public StateTransition(T initialState)
        {
            CurrentState = initialState;
        }
    }
}