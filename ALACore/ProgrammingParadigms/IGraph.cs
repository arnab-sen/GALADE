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
    /// <para>It also has several operation functions, to add/delete nodes and edges.</para>
    /// <para>Each node and edge should have a unique associated string id.
    /// It is up to the class implementing IGraph to decide how it wants to store the objects themselves.
    /// A simple Dictionary&lt;string, object&gt; is recommended, although not enforced, to allow for the objects to be processed
    /// in some way (e.g. type casting or sorting) before being stored.</para>
    /// </summary>
    public interface IGraph
    {
        List<object> Nodes { get; }
        List<object> Edges { get; }

        void AddNode(object node);
        bool ContainsNode(object node);
        void DeleteNode(object node);

        void AddEdge(object edge);
        bool ContainsEdge(object edge);
        void DeleteEdge(object edge);
    }
}
