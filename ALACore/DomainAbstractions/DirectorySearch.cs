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
    /// <para>1. IDataFlow&lt;string&gt; rootFilePath:</para>
    /// <para>2. IDataFlow&lt;Dictionary&lt;string, List&lt;string&gt;&gt;&gt; foundDirectoriesOutput:</para>
    /// <para>3. IDataFlow&lt;Dictionary&lt;string, List&lt;string&gt;&gt;&gt; foundFilesOutput:</para>
    /// </summary>
    public class DirectorySearch : IDataFlow<string> // rootFilePath
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string FilenameFilter { get; set; } = "*.*";

        // Private fields
        private string rootFilePath;
        private HashSet<string> desiredDirectories = new HashSet<string>();
        private Dictionary<string, List<string>> _foundDirectories = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _foundFilesByDir = new Dictionary<string, List<string>>();
        private List<string> _allFoundFiles = new List<string>();

        // Ports
        private IDataFlow<Dictionary<string, List<string>>> foundDirectories;
        private IDataFlow<Dictionary<string, List<string>>> foundFilesByDir;
        private IDataFlow<List<string>> allFoundFiles;

        public DirectorySearch(string[] directoriesToFind = null)
        {
            var directories = directoriesToFind ?? new string[] { };

            foreach (var s in directories)
            {
                desiredDirectories.Add(s);
            }
        }

        private void Search(DirectoryInfo rootDirectory)
        {
            if (desiredDirectories.Count == 0 || desiredDirectories.Contains(rootDirectory.Name))
            {
                if (!_foundDirectories.ContainsKey(rootDirectory.Name))
                {
                    _foundDirectories[rootDirectory.Name] = rootDirectory.GetDirectories().Select(s => s.FullName).ToList();
                }
                else
                {
                    _foundDirectories[rootDirectory.Name].AddRange(rootDirectory.GetDirectories().Select(s => s.FullName).ToList());
                }

                if (!_foundFilesByDir.ContainsKey(rootDirectory.Name))
                {
                    _foundFilesByDir[rootDirectory.Name] = rootDirectory.GetFiles(FilenameFilter).Select(s => s.FullName).ToList();
                }
                else
                {
                    _foundFilesByDir[rootDirectory.Name].AddRange(rootDirectory.GetFiles(FilenameFilter).Select(s => s.FullName).ToList());
                }
            }

            _allFoundFiles.AddRange(rootDirectory.GetFiles(FilenameFilter).Select(s => s.FullName).ToList());

            var directories = rootDirectory.GetDirectories();

            foreach (var directory in directories)
            {
                Search(directory);
            }
        }

        private void Output()
        {
            if (foundDirectories != null) foundDirectories.Data = _foundDirectories;
            if (foundFilesByDir != null) foundFilesByDir.Data = _foundFilesByDir;
            if (allFoundFiles != null) allFoundFiles.Data = _allFoundFiles;
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => rootFilePath;
            set
            {
                _foundFilesByDir.Clear();
                _foundDirectories.Clear();
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
