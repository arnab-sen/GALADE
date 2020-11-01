using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Searches a root directory for all matching directories according to a given filter.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;Dictionary&lt;string, List&lt;string&gt;&gt;&gt; foundDirectoriesOutput:.</para>
    /// <para>2. IDataFlow&lt;Dictionary&lt;string, List&lt;string&gt;&gt;&gt; foundFilesOutput.</para>
    /// </summary>
    public class DirectorySearch : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string FilenameFilter { get; set; } = "*.*";

        // Private fields
        private string rootFilePath;
        private HashSet<string> desiredDirectories = new HashSet<string>();
        private Dictionary<string, List<string>> foundDirectories = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> foundFiles = new Dictionary<string, List<string>>();

        // Ports
        private IDataFlow<Dictionary<string, List<string>>> foundDirectoriesOutput;
        private IDataFlow<Dictionary<string, List<string>>> foundFilesOutput;

        public DirectorySearch(params string[] directoriesToFind)
        {
            foreach (var s in directoriesToFind)
            {
                desiredDirectories.Add(s);
            }
        }

        private void Search(DirectoryInfo rootDirectory)
        {
            if (desiredDirectories.Contains(rootDirectory.Name))
            {
                foundDirectories[rootDirectory.Name] = rootDirectory.GetDirectories().Select(s => s.FullName).ToList();
                foundFiles[rootDirectory.Name] = rootDirectory.GetFiles(FilenameFilter).Select(s => s.FullName).ToList();
            }

            var directories = rootDirectory.GetDirectories();
            foreach (var directory in directories)
            {
                Search(directory);
            }
        }

        private void Output()
        {
            if (foundDirectoriesOutput != null) foundDirectoriesOutput.Data = foundDirectories;
            if (foundFilesOutput != null) foundFilesOutput.Data = foundFiles;
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => rootFilePath;
            set
            {
                rootFilePath = value;

                if (Directory.Exists(value))
                {
                    var root = new DirectoryInfo(Path.GetFullPath(rootFilePath));
                    Search(root);

                    Output(); 
                }
            }
        }
    }
}
