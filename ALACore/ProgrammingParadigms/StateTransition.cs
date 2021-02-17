using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StateTransition<T> where T : Enum
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public T CurrentState { get; private set; }
        public Tuple<T, T> LatestTransition { get; private set; }
        public Func<T, T, bool> Matches { get; set; }
        public delegate void StateTransitionedDelegate(Tuple<T, T> transition);
        public event StateTransitionedDelegate StateChanged;
        public event StateTransitionedDelegate StateRefreshed;

        // Private fields

        public void Update(T newState)
        {
            LatestTransition = Tuple.Create(CurrentState, newState);

            CurrentState = newState;

            if (!newState.Equals(CurrentState))
            {
                StateChanged?.Invoke(LatestTransition);
                Logging.Log($"StateTransition: State changed from {LatestTransition.Item1} to {LatestTransition.Item2}");
            }
            else
            {
                StateRefreshed?.Invoke(LatestTransition);
                // Logging.Log($"StateTransition: State {LatestTransition.Item2} refreshed");
            }
        }

        public bool CurrentStateMatches(T flag)
        {
            return Matches?.Invoke(flag, CurrentState) ?? false;
            // return (CurrentState & flag) != 0; // For enums with the [Flags] attribute only
        }

        public StateTransition(T initialState)
        {
            CurrentState = initialState;
        }
    }
}