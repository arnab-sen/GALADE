using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Microsoft.Win32;

namespace RequirementsAbstractions
{
    public class AbstractionModelManager
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private FileBrowser _fileBrowser;
        private FolderBrowser folderBrowser;

        // Ports

        // Methods
        public void OpenFile()
        {
            (_fileBrowser as IEvent).Execute();
        }

        public AbstractionModelManager()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            EventConnector id_a28e1e7929f14f148f5e7c011ceb1b92 = new EventConnector() {  };
            FileBrowser fileBrowser = new FileBrowser() { InstanceName = "fileBrowser", Mode = "Open" };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            id_a28e1e7929f14f148f5e7c011ceb1b92.WireTo(fileBrowser, "fanoutList");
            // END AUTO-GENERATED WIRING

            _fileBrowser = fileBrowser;
        }
    }
}




