using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace RequirementsAbstractions
{
    public class ALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Type { get; set; } = "?";
        public string Name { get; set; } = "";
        public List<string> ProgrammingParadigms = new List<string>();
        public List<string> DomainAbstractions = new List<string>();
        public Graph Graph { get; set; }
        
        // Private fields

        // Ports

        // Methods

        public ALANode()
        {

        }
    }
}
