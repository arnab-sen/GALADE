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
        private Dictionary<string, object> _nodeDictionary = new Dictionary<string, object>();
        private Dictionary<string, object> _edgeDictionary = new Dictionary<string, object>();
        private HashSet<string> _nodeIds = new HashSet<string>();
        private HashSet<string> _edgeIds = new HashSet<string>();

        // Ports

        // IGraph implementation
        HashSet<string> IGraph.NodeIds => _nodeDictionary.Keys.ToHashSet();

        HashSet<string> IGraph.EdgeIds => _edgeDictionary.Keys.ToHashSet();

        public void AddNode(string id, object node)
        {
            if (!ContainsNode(id))
            {
                _nodeDictionary[id] = node;
            }
            else
            {
                throw new Exception($"Error in AddNode: Graph already contains a node with id {id}.");
            }
        }
        public bool ContainsNode(string id) => _nodeDictionary.ContainsKey(id);
        public object GetNode(string id) => ContainsNode(id)  ? _nodeDictionary[id] : null;
        public void DeleteNode(string id)
        {
            if (ContainsNode(id))
            {
                _nodeDictionary.Remove(id);
            }
            else
            {
                throw new Exception($"Error in DeleteNode: Graph does not contain a node with id {id}.");
            }
        }

        public void AddEdge(string id, object edge)
        {
            if (!ContainsEdge(id))
            {
                _edgeDictionary[id] = edge;
            }
            else
            {
                throw new Exception($"Error in AddEdge: Graph already contains an edge with id {id}.");
            }
        }

        public bool ContainsEdge(string id) => _edgeDictionary.ContainsKey(id);

        public object GetEdge(string id) => ContainsEdge(id) ? _edgeDictionary[id] : null;

        public void DeleteEdge(string id)
        {
            if (ContainsEdge(id))
            {
                _edgeDictionary.Remove(id);
            }
            else
            {
                throw new Exception($"Error in DeleteEdge: Graph does not contain an edge with id {id}.");
            }
        }


        // Methods


        public Graph()
        {

        }
    }
}
