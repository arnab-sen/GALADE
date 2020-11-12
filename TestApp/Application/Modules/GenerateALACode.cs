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
        private List<string> _instantiations;
        private List<string> _wireTos;
        private List<string> _allCode;

        // Ports
        private IDataFlow<List<string>> instantiations;
        private IDataFlow<List<string>> wireTos;
        private IDataFlow<List<string>> allCode;

        void IEvent.Execute()
        {
            _instantiations = GenerateInstantiations();
            _wireTos = GenerateWireTos();

            _allCode = new List<string>();
            _allCode.AddRange(_instantiations);
            _allCode.AddRange(_wireTos);

            if (instantiations != null) instantiations.Data = _instantiations;
            if (wireTos != null) wireTos.Data = _wireTos;
            if (allCode != null) allCode.Data = _allCode;
        }

        // Methods
        public List<string> GenerateInstantiations()
        {
            _instantiations = new List<string>();

            var nodes = Graph.Nodes;

            foreach (var node in nodes)
            {
                var alaNode = node as ALANode;
                if (alaNode == null) continue;

                _instantiations.Add(alaNode.ToInstantiation());
            }

            return _instantiations;
        }

        public List<string> GenerateWireTos()
        {
            _wireTos = new List<string>();

            var edges = Graph.Edges.Where(e => e is ALAWire wire && wire.Source != null && wire.Destination != null);

            foreach (var edge in edges)
            {
                var wire = edge as ALAWire;
                if (wire == null) continue;

                var portName = (wire.SourcePort.Payload as Port)?.Name ?? "";
                if (string.IsNullOrWhiteSpace(portName))
                {
                    _wireTos.Add($"{wire.Source.Name}.WireTo({wire.Destination.Name});");
                }
                else
                {
                    _wireTos.Add($"{wire.Source.Name}.WireTo({wire.Destination.Name}, \"{(wire.SourcePort.Payload as Port)?.Name}\");");
                }
            }

            return _wireTos;
        }


        public GenerateALACode()
        {

        }
    }
}
