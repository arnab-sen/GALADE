using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public class Graph : IGraph
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields

        // Ports

        // IGraph implementation
        public HashSet<object> Nodes { get; } = new HashSet<object>();
        public HashSet<object> Edges { get; } = new HashSet<object>();


        public void AddNode(object node)
        {
            if (!ContainsNode(node))
            {
                Nodes.Add(node);
            }
        }
        public bool ContainsNode(object node) => Nodes.Contains(node);
        public void DeleteNode(object node)
        {
            if (ContainsNode(node))
            {
                Nodes.Remove(node);
            }
        }

        public void AddEdge(object edge)
        {
            if (!ContainsEdge(edge))
            {
                Edges.Add(edge);
            }
        }

        public bool ContainsEdge(object edge) => Edges.Contains(edge);


        public void DeleteEdge(object edge)
        {
            if (ContainsEdge(edge))
            {
                Edges.Remove(edge);
            }

        }


        // Methods


        public Graph()
        {

        }
    }
}
