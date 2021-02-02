using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class LookupTable<T1, T2> : IDataFlow<Tuple<T1, T2>>, IDataFlow<T1> // pair, key
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public Dictionary<T1, T2> InitialDictionary
        {
            get => _dictionary;
            set => _dictionary = new Dictionary<T1, T2>(value);
        }

        // Private fields
        private Dictionary<T1, T2> _dictionary = new Dictionary<T1, T2>();
        private bool initialisedWithNonEmptyDictionary = false;

        // Ports
        private IDataFlowB<Dictionary<T1, T2>> initialDictionaryInput;
        private IDataFlowB<Dictionary<T1, T2>> dictionaryInput;
        private IDataFlow<Dictionary<T1, T2>> dictionaryOutput;
        private IDataFlow<T2> valueOutput;

        private void Initialise()
        {
            if (initialDictionaryInput?.Data != null && !initialisedWithNonEmptyDictionary) // Dictionary to use as a base
            {
                _dictionary = new Dictionary<T1, T2>(initialDictionaryInput.Data);
                initialisedWithNonEmptyDictionary = true;
            }

            if (dictionaryInput != null) _dictionary = dictionaryInput.Data; // Dictionary to modify
        }

        public LookupTable()
        {

        }

        // IDataFlow<Tuple<T1, T2>> implementation
        Tuple<T1, T2> IDataFlow<Tuple<T1, T2>>.Data
        {
            get => default;
            set
            {
                Initialise();

                _dictionary[value.Item1] = value.Item2;

                if (dictionaryOutput != null) dictionaryOutput.Data = _dictionary;
            }
        }

        // IDataFlow<T1> implementation
        T1 IDataFlow<T1>.Data
        {
            get => default;
            set
            {
                Initialise();

                if (valueOutput != null && _dictionary.ContainsKey(value)) valueOutput.Data = _dictionary[value];
            }
        }
    }
}
