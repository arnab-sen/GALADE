using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Watches a folder for any changes in its files, and outputs the path of any file changed.
    /// A file must be altered and then saved to register as a change.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; rootPath: The folder path watch. The watcher starts when it receives this string.</para>
    /// <para>2. IDataFlow&lt;string&gt; changedFile: The filepath of the latest changed file.</para>
    /// </summary>
    public class FolderWatcher : IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public string RootPath
        {
            get => _watcher.Path;
            set => _watcher.Path = value;
        }

        public string Filter
        {
            get => _watcher.Filter;
            set => _watcher.Filter = value;
        }

        public string PathRegex { get; set; }

        public bool WatchSubdirectories
        {
            get => _watcher.IncludeSubdirectories;
            set => _watcher.IncludeSubdirectories = value;
        }

        public bool IsWatching => _watcher.EnableRaisingEvents;

        // Private fields
        private FileSystemWatcher _watcher = new FileSystemWatcher();
        
        // Ports
        private IDataFlow<string> changedFile;

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => _watcher.Path;
            set
            {
                _watcher.Path = value;
                StartWatching();
            }
        }

        // Methods
        public void StartWatching()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            _watcher.EnableRaisingEvents = false;
        }

        public void Output(string path)
        {
            if (changedFile != null) changedFile.Data = path;
        }

        public bool IsMatch(string path) => string.IsNullOrEmpty(PathRegex) || Regex.IsMatch(path, PathRegex);

        public FolderWatcher()
        {
            _watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.Size;
            _watcher.Filter = Filter;
            _watcher.IncludeSubdirectories = true;

            _watcher.Changed += (sender, args) =>
            {
                if (IsMatch(args.FullPath))
                {
                    Logging.Log($"Changed: {args.FullPath}");
                    Output(args.FullPath); 
                }
            };

            _watcher.Renamed += (sender, args) =>
            {
                if (IsMatch(args.FullPath))
                {
                    Logging.Log($"Renamed: [{args.OldName} at {args.OldFullPath}] to [{args.Name} at {args.FullPath}]");
                    Output(args.FullPath); 
                }
            };

            _watcher.Created += (sender, args) =>
            {
                if (IsMatch(args.FullPath))
                {
                    Logging.Log($"Created: {args.FullPath}");
                    Output(args.FullPath); 
                }
            };

            _watcher.Deleted += (sender, args) =>
            {
                if (IsMatch(args.FullPath))
                {
                    Logging.Log($"Deleted: {args.FullPath}");
                    Output(args.FullPath); 
                }
            };
        }
    }
}
