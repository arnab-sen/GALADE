using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using Microsoft.Win32;
using System.IO;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Opens a window that lets the user select a file.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent open : Opens the browser.</para>
    /// <para>2. IDataFlow&lt;string&gt; selectedFilePathOutput : The selected file path.</para>
    /// </summary>
    public class FileBrowser : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Mode { get; set; } = "Open";
        public string Filter { get; set; }
        public string DefaultPath { get; set; }

        // Private fields
        private OpenFileDialog _openBrowser = new OpenFileDialog();
        private SaveFileDialog _saveBrowser = new SaveFileDialog();

        // Ports
        private IDataFlow<string> selectedFilePathOutput;

        public FileBrowser()
        {
            _openBrowser.FileOk += (sender, args) =>
            {
                if (File.Exists(_openBrowser.FileName))
                {
                    Output(_openBrowser.FileName);
                }
            };

            _saveBrowser.FileOk += (sender, args) =>
            {
                Output(_saveBrowser.FileName);
            };

        }

        private void Output(string content)
        {
            if (selectedFilePathOutput != null) selectedFilePathOutput.Data = content; 
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            var mode = Mode.ToLower();
            bool defaultPathIsValid = !string.IsNullOrEmpty(DefaultPath) && Directory.Exists(Path.GetDirectoryName(DefaultPath));

            if (mode == "open")
            {
                if (!string.IsNullOrEmpty(Filter)) _openBrowser.Filter = Filter;
                if (defaultPathIsValid) _openBrowser.InitialDirectory = DefaultPath;

                _openBrowser.ShowDialog();
            }
            else if (mode == "save")
            {
                if (!string.IsNullOrEmpty(Filter)) _saveBrowser.Filter = Filter;
                if (defaultPathIsValid) _saveBrowser.InitialDirectory = DefaultPath;

                _saveBrowser.ShowDialog();
            }
        }
    }
}
