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
    /// <para>Writes a string to a file.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; filePathInput : The input path. If the file doesn't exist, nothing will happen.</para>
    /// </summary>
    public class FileWriter : IDataFlow<string> // filePathInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string FilePath { get; set; } = "";
        public bool Append { get; set; } = false;

        // Private fields
        private string fileContent = "";

        // Ports
        private IDataFlowB<string> filePathInput;
        private IEvent complete;

        /// <summary>
        /// <para>Writes a string to a file.</para>
        /// </summary>
        public FileWriter()
        {

        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => FilePath;
            set
            {
                if (filePathInput != null) FilePath = filePathInput.Data;

                fileContent = value;

                if (Append)
                {
                    File.AppendAllText(FilePath, Environment.NewLine);
                    File.AppendAllText(FilePath, fileContent);
                }
                else // Overwrite file contents
                {
                    File.WriteAllText(FilePath, fileContent);
                }

                complete?.Execute();
            }
        }
    }
}
