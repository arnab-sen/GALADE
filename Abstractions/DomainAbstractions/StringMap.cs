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
    /// <para>Given an input string and two input lists of strings, this replaces every instance of oldList[i] in the input string with newList[i].</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start:</para>
    /// <para>2. IDataFlowB&lt;string&gt; contentToEditInput:</para>
    /// <para>3. IDataFlowB&lt;List&lt;string&gt;&gt; oldSetInput:</para>
    /// <para>4. IDataFlowB&lt;List&lt;string&gt;&gt; newSetInput:</para>
    /// <para>4. IDataFlow&lt;string&gt; newStringOutput;:</para>
    /// </summary>
    public class StringMap : IEvent
    {
        // Public fields and properties
        public string InstanceName = "Default";
        
        // Private fields
        private string _oldString = "";
        private string _newString = "";
        
        // Ports
        private IDataFlowB<string> contentToEditInput;
        private IDataFlowB<List<string>> oldListInput;
        private IDataFlowB<List<string>> newListInput;
        private IDataFlow<string> newStringOutput;
        
        // IEvent implementation
        void IEvent.Execute()
        {
            if (contentToEditInput != null && oldListInput != null && newListInput != null && oldListInput.Data.Count == newListInput.Data.Count)
            {
                _oldString = contentToEditInput.Data;
                _newString = _oldString;

                var oldList = oldListInput.Data.Select(s => s).ToList();
                var newList = newListInput.Data.Select(s => s).ToList();

                for (int i = 0; i < oldList.Count(); i++)
                {
                    _newString = _newString.Replace(oldList[i], newList[i]);
                }
            }

            if (newStringOutput != null) newStringOutput.Data = _newString;
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public StringMap()
        {
            
        }
    }
}
