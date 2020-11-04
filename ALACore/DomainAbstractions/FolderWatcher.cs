using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Watches a folder for any changes in its files, and outputs the path of any file changed.
    /// A file must be altered and then saved to register as a change.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent onOffToggle: Toggles between the watching/not watching states.</para>
    /// <para>2. IDataFlow&lt;string&gt; changedFile: The filepath of the latest changed file.</para>
    /// </summary>
    public class FolderWatcher : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public string RootPath
        {
            get => _watcher.Path;
            set => _watcher.Path = value;
        }

        public string Filter { get; set; } = "*.*";

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

        // IEvent implementation
        void IEvent.Execute()
        {
            if (!_watcher.EnableRaisingEvents)
            {
                StartWatching();
            }
            else
            {
                StopWatching();
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

        public FolderWatcher()
        {
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Filter = Filter;
            _watcher.IncludeSubdirectories = true;

            _watcher.Changed += (sender, args) =>
            {
                if (changedFile != null) changedFile.Data = args.FullPath;
            };
        }
    }
}
