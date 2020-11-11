using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using RequirementsAbstractions;

namespace Application
{
    public class GenerateALACode : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Graph Graph { get; set; }

        // Private fields

        // Ports
        private IDataFlow<List<string>> instantiations;
        private IDataFlow<List<string>> wireTos;

        void IEvent.Execute()
        {
            if (instantiations != null) instantiations.Data = GenerateInstantiations();
            if (wireTos != null) wireTos.Data = GenerateWireTos();
        }

        // Methods
        public List<string> GenerateInstantiations()
        {
            var instantiations = new List<string>();

            var nodes = Graph.Nodes.OfType<ALANode>().ToList();

            foreach (var node in nodes)
            {
                instantiations.Add(node.ToInstantiation());
            }

            return instantiations;
        }

        public List<string> GenerateWireTos()
        {
            var wireTos = new List<string>();

            return wireTos;
        }


        public GenerateALACode()
        {

        }
    }
}
