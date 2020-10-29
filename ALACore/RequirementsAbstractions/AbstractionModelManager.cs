using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Microsoft.Win32;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public AbstractionModel CreateAbstractionModel(string code)
        {
            var model = new AbstractionModel();

            var parser = new CodeParser();

            var classNode = parser.GetClasses(code).First() as ClassDeclarationSyntax;
            var className = classNode.Identifier.Value;
            return model;
        }

        public AbstractionModelManager()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            EventConnector id_f259bc78ad7042968bc454b2e850b121 = new EventConnector() {  };
            FileBrowser fileBrowser = new FileBrowser() { InstanceName = "fileBrowser", Mode = "Open" };
            ApplyAction<string> id_2ae06919ade84cd0875503e326be84ea = new ApplyAction<string>() { Lambda = input =>{CreateAbstractionModel(input);} };
            FileReader id_81edc5b4fb9b41b0b909267dfd13b526 = new FileReader() {  };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            id_f259bc78ad7042968bc454b2e850b121.WireTo(fileBrowser, "fanoutList");
            fileBrowser.WireTo(id_81edc5b4fb9b41b0b909267dfd13b526, "selectedFilePathOutput");
            id_81edc5b4fb9b41b0b909267dfd13b526.WireTo(id_2ae06919ade84cd0875503e326be84ea, "fileContentOutput");
            // END AUTO-GENERATED WIRING

            _fileBrowser = fileBrowser;
        }
    }
}










