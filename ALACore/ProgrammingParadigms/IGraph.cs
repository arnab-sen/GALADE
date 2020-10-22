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
    /// A simple Dictionary&lt;string, object&gt; is recommended, although not enforced, to allow for the objects to be processed
    /// in some way (e.g. type casting or sorting) before being stored.</para>
    /// </summary>
    public interface IGraph
    {
        HashSet<string> NodeIds { get; }
        HashSet<string> EdgeIds { get; }

        void AddNode(string id, object node);
        bool ContainsNode(string id);
        object GetNode(string id);
        void DeleteNode(string id);

        void AddEdge(string id, object edge);
        bool ContainsEdge(string id);
        object GetEdge(string id);
        void DeleteEdge(string id);
    }
}
