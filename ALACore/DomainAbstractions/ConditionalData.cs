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
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConditionalData<T> : IDataFlow<T> // data
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Predicate<T> Condition;

        // Private fields
        private T _data;

        // Ports
        private IDataFlow<T> conditionMetOutput;
        private IDataFlow<T> conditionNotMetOutput;

        public ConditionalData()
        {

        }

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => _data;
            set
            {
                _data = value;

                if (Condition != null)
                {
                    bool meetsCondition = Condition(_data);

                    if (meetsCondition)
                    {
                        if (conditionMetOutput != null) conditionMetOutput.Data = _data;
                    }
                    else
                    {
                        if (conditionNotMetOutput != null) conditionNotMetOutput.Data = _data;
                    } 
                }
            }
        }
    }
}
