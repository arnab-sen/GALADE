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

        public delegate void GraphChangedDelegate(object graphElement);
        public GraphChangedDelegate NodeAdded;
        public GraphChangedDelegate NodeDeleted;
        public GraphChangedDelegate EdgeAdded;
        public GraphChangedDelegate EdgeDeleted;

        // Private fields
        private Dictionary<string, object> _payload { get; } = new Dictionary<string, object>();

        // Ports

        // IGraph implementation
        public List<object> Nodes { get; } = new List<object>();
        public List<object> Edges { get; } = new List<object>();
        public List<object> Roots { get; } = new List<object>();

        public void AddNode(object node)
        {
            if (!ContainsNode(node))
            {
                Nodes.Add(node);
                NodeAdded?.Invoke(node);
            }
        }
        public bool ContainsNode(object node) => Nodes.Contains(node);
        public void DeleteNode(object node)
        {
            if (ContainsNode(node))
            {
                Nodes.Remove(node);
                NodeDeleted?.Invoke(node);
            }
            if (Roots.Contains(node))
            {
                Roots.Remove(node);
            }
        }

        public void AddEdge(object edge)
        {
            if (!ContainsEdge(edge))
            {
                Edges.Add(edge);
                EdgeAdded?.Invoke(edge);
            }
        }

        public bool ContainsEdge(object edge) => Edges.Contains(edge);


        public void DeleteEdge(object edge)
        {
            if (ContainsEdge(edge))
            {
                Edges.Remove(edge);
                EdgeDeleted?.Invoke(edge);
            }
        }

        // Methods
        public object Get(string key) => _payload.ContainsKey(key) ? _payload[key] : null;
        public void Set(string key, object value) => _payload[key] = value;

        public Graph()
        {

        }
    }
}
