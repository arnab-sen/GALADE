using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProgrammingParadigms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Serializes an input of type T into JSON and outputs it as a string, which is also written to a file if a valid path is provided. </para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;T&gt; input: The object to be serialized.</para>
    /// <para>2. IDataFlow&lt;string&gt; stringOutput: The input object serialized to a string.</para>
    /// </summary>
    public class JSONWriter<T> : IDataFlow<T> // input
    {
        // Properties
        public string InstanceName = "Default";
        public string FilePath { get; set; } = "";

        public bool IndentString { get; set; } = true;

        // Private fields
        private T lastValue;
        private string json = "";

        // Ports
        private IDataFlow<string> stringOutput;

        /// <summary>
        /// <para>Serializes an input of type T into JSON and outputs it as a string, which is also written to a file if a valid path is provided. </para>
        /// </summary>
        /// <param name="path"></param>
        public JSONWriter(string path = "")
        {
            if (!string.IsNullOrEmpty(path)) FilePath = path;
        }

        private void OutputAsString()
        {
            if (stringOutput != null) stringOutput.Data = json;
        }

        private void WriteToFile(string path)
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                File.WriteAllText(path, json);
            }
        }

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => lastValue;
            set
            {
                json = JsonConvert.SerializeObject(value, IndentString ? Formatting.Indented : Formatting.None);

                OutputAsString();

                WriteToFile(FilePath);
            }
        }
    }
}
