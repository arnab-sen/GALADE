using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using Newtonsoft.Json.Linq;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Edits the input JSON content at a given JSONPath, and outputs the edited JSON.</para>
    /// <para>For more information on JSONPaths, see:</para>
    /// <para>https://www.newtonsoft.com/json/help/html/QueryJsonSelectToken.html</para>
    /// <para>https://goessner.net/articles/JsonPath/index.html</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; newContentInput:</para>
    /// <para>2. IDataFlowB&lt;string&gt; jsonInput:</para>
    /// <para>3. IDataFlow&lt;string&gt; newJsonOutput:</para>
    /// </summary>
    public class JSONEditor : IDataFlow<object> // newContentInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string JSONPath { get; set; }

        // Private fields
        private string _json = "";
        private object _newContent;
        
        // Ports
        private IDataFlowB<string> jsonInput;
        private IDataFlow<string> newJsonOutput;
        
        // IDataFlow<object> implementation
        object IDataFlow<object>.Data
        {
            get => _newContent;
            set
            {
                if (!string.IsNullOrEmpty(JSONPath) && jsonInput != null && jsonInput.Data != null)
                {
                    _json = jsonInput.Data;
                    _newContent = value;

                    var obj = JToken.Parse(_json);
                    foreach (var token in obj.SelectTokens(JSONPath))
                    {
                        token.Replace(JToken.FromObject(_newContent));
                    }

                    if (newJsonOutput != null) newJsonOutput.Data = obj.ToString();
                }

            }
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public JSONEditor()
        {
            
        }
    }
}
