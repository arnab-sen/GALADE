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
    public class FileReader : IDataFlow<string> // filePathInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

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

        public string ReadFile(string path)
        {
            if (File.Exists(path))
            {
                filePath = path;
                fileContent = File.ReadAllText(filePath);
                if (fileContentOutput != null) fileContentOutput.Data = fileContent;
                return fileContent;
            }
            else
            {
                Logging.Log($"File does not exist at {path}");
                return "";
            }
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => filePath;
            set => ReadFile(value);
        }
    }
}
