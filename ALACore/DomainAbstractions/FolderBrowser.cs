using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using System.Windows.Forms;
using System.IO;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Opens a window that lets the user select a folder.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent open : Opens the browser.</para>
    /// <para>2. IDataFlow&lt;string&gt; selectedFolderPathOutput : The selected folder path.</para>
    /// </summary>
    public class FolderBrowser : IEvent // open
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public string Description
        {
            get => browser.Description;
            set => browser.Description = value;
        }

        // Private fields
        private FolderBrowserDialog browser = new FolderBrowserDialog(); // There is no folder browser in WPF, so the one from Windows Forms is being used

        // Ports
        private IDataFlow<string> selectedFolderPathOutput;

        public FolderBrowser()
        {

        }

        // IEvent implementation
        void IEvent.Execute()
        {
            if (browser.ShowDialog() == DialogResult.OK)
            {
                if (selectedFolderPathOutput != null) selectedFolderPathOutput.Data = browser.SelectedPath; 
            }
        }
    }
}
