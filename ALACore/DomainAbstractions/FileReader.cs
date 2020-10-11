using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using System.IO;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Reads and outputs the contents of a file as a string.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; filePathInput : The input path. If the file doesn't exist, nothing will happen.</para>
    /// <para>2. IDataFlow&lt;string&gt; fileContentOutput : The file contents as a string.</para>
    /// </summary>
    public class FileReader : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName = "Default";

        // Private fields
        private string filePath = "";
        private string fileContent = "";

        // Ports
        private IDataFlow<string> fileContentOutput;

        /// <summary>
        /// <para>Reads and outputs the contents of a file as a string.</para>
        /// </summary>
        public FileReader()
        {

        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => filePath;
            set
            {
                if (File.Exists(value))
                {
                    filePath = value;
                    fileContent = File.ReadAllText(filePath);
                    if (fileContentOutput != null) fileContentOutput.Data = fileContent;
                }
            }
        }
    }
}
