using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// <para>A generic graph data structure. Stores a set of node ids and a set of edge ids.
    /// Nodes and edges are passed as objects, to keep IGraph abstract and decoupled.</para>
    /// <para>It also has several operation functions, to add/get/delete nodes and edges.</para>
    /// <para>Each node and edge should have a unique associated string id.
    /// It is up to the class implementing IGraph to decide how it wants to store the objects themselves.
    /// A simple Dictionary&lt;string, object&gt; is recommended, although not enforced, to allow for the objects to be cast before being stored.</para>
    /// </summary>
    public interface IGraph
    {
        HashSet<string> NodeIds { get; set; }
        HashSet<string> EdgeIds { get; set; }

        void AddNode(string id, object node);
        object GetNode(string id);
        void DeleteNode(string id, object node);

        void AddEdge(string id, object edge);
        object GetEdge(string id);
        void DeleteEdge(string id, object edge);
    }
}
