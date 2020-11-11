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

            var nodes = Graph.Nodes;

            foreach (var node in nodes)
            {
                var alaNode = node as ALANode;
                if (alaNode == null) continue;

                instantiations.Add(alaNode.ToInstantiation());
            }

            return instantiations;
        }

        public List<string> GenerateWireTos()
        {
            var wireTos = new List<string>();

            var edges = Graph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null);

            foreach (var edge in edges)
            {
                var wire = edge as ALAWire;
                if (wire == null) continue;

                var portName = (wire.SourcePort.Payload as Port)?.Name ?? "";
                if (string.IsNullOrWhiteSpace(portName))
                {
                    wireTos.Add($"{wire.Source.Name}.WireTo({wire.Destination.Name});");
                }
                else
                {
                    wireTos.Add($"{wire.Source.Name}.WireTo({wire.Destination.Name}, \"{(wire.SourcePort.Payload as Port)?.Name}\");");
                }
            }

            return wireTos;
        }


        public GenerateALACode()
        {

        }
    }
}
