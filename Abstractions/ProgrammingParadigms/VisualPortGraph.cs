using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;

namespace ProgrammingParadigms
{
    /// <summary>
    /// <para></para>
    /// </summary>
    public class VisualPortGraph : IMemento
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        public Canvas MainCanvas { get; set; }

        public IVisualPortGraphNode Root
        {
            get => _root;
            set
            {
                _root = value;
                _selectedNodeId = _root?.Id ?? "";
            }
        }

        public bool DebugOutputAll { get; set; } = false;

        public enum ZIndex
        {
            Node = 0,
            Wire = 1,
            WireHandle = 2
        }

        public List<string> NodeTypes { get; set; } = new List<string>();

        // Private fields
        private Dictionary<string, IPortConnection> _portConnections = new Dictionary<string, IPortConnection>();
        private IVisualPortGraphNode _root;
        private Dictionary<string, IVisualPortGraphNode> _nodesById { get; } = new Dictionary<string, IVisualPortGraphNode>();
        private string _selectedNodeId { get; set; }
        private string _selectedConnectionId { get; set; }
        private HashSet<string> _selectedNodeIds { get; } = new HashSet<string>();
        private HashSet<string> _selectedConnectionIds { get; } = new HashSet<string>();
        private Dictionary<string, string> _treeParents = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _adjLists = new Dictionary<string, List<string>>(); // For more efficient graph traversal
        private string _latestNodeId = "";

        // Methods
        public IVisualPortGraphNode GetRoot() => Root;

        public bool Contains(IVisualPortGraphNode node) => node != null && _nodesById.ContainsKey(node.Id);
        public bool Contains(IPortConnection connection) => connection != null && _portConnections.ContainsKey(connection.Id);

        public IVisualPortGraphNode GetNode(string id)
        {
            return _nodesById.ContainsKey(id) ? _nodesById[id] : null;
        }

        public IEnumerable<string> GetNodeIds() => _nodesById.Keys;
        public IEnumerable<IVisualPortGraphNode> GetNodes() => _nodesById.Values;

        public IEnumerable<IVisualPortGraphNode> GetNodes(IEnumerable<string> ids)
        {
            return ids.Select(GetNode);
        }

        public IVisualPortGraphNode GetLatestNode() => GetNode(_latestNodeId);

        public IPortConnection GetConnection(string id) => _portConnections.ContainsKey(id) ? _portConnections[id] : null;
        public IEnumerable<IPortConnection> GetConnections()
        {
            // Get all visible connections from all output ports in the graph
            var nodes = GetNodes();
            var connectionSet = new HashSet<string>();
            var connections = new List<IPortConnection>();

            foreach (var node in nodes)
            {
                foreach (var port in node.Ports)
                {
                    foreach (var cxnId in port.ConnectionIds)
                    {
                        if (!connectionSet.Contains(cxnId))
                        {
                            connectionSet.Add(cxnId);
                            connections.Add(GetConnection(cxnId));
                        }
                    }
                }
            }

            return connections;
        }

        public void AddNode(IVisualPortGraphNode node)
        {
            if (node == null) return;

            if (!Contains(node))
            {
                if (string.IsNullOrEmpty(node.Id)) node.Id = Utilities.GetUniqueId();
                _nodesById[node.Id] = node;
                if (Root == null) Root = node;
                MainCanvas.Children.Add(node.Render);
                Canvas.SetZIndex(node.Render, (int)ZIndex.Node);

                _adjLists[node.Id] = new List<string>();

                _latestNodeId = node.Id;
            }
            else
            {
                Logging.Log($"Failed to add IPortGraphNode {node.Id} to PortGraph. Please ensure that the node has a unique Id.");
            }
        }

        public void AddConnection(IPortConnection connection, bool undoable = true)
        {
            if (!Contains(connection))
            {
                _portConnections[connection.Id] = connection;

                if (!MainCanvas.Children.Contains(connection.Render)) MainCanvas.Children.Add(connection.Render);
                Canvas.SetZIndex(connection.Render, (int)VisualPortGraph.ZIndex.Wire);
                if (!MainCanvas.Children.Contains(GetNode(connection.SourceId).Render)) MainCanvas.Children.Add(GetNode(connection.SourceId).Render);

                if (!string.IsNullOrEmpty(connection.DestinationId))
                {
                    _adjLists[connection.SourceId].Add(connection.DestinationId);
                    if (!MainCanvas.Children.Contains(GetNode(connection.DestinationId).Render)) MainCanvas.Children.Add(GetNode(connection.DestinationId).Render);
                    if (string.IsNullOrEmpty(GetTreeParent(connection.DestinationId))) _treeParents[connection.DestinationId] = connection.SourceId;
                }

                if (undoable) ActionPerformed?.Invoke(this); 
            }
        }

        public void DeleteConnection(IPortConnection connection, bool undoable = false)
        {
            if (Contains(connection))
            {
                // _portConnections.Remove(connection.Id); // Don't delete from memory, just hide

                connection.Deselect();
                connection.Render.Visibility = Visibility.Collapsed;

                connection.SourcePort?.ConnectionIds.Remove(connection.Id);
                connection.DestinationPort?.ConnectionIds.Remove(connection.Id);

                if (GetTreeParent(connection.DestinationId) == connection.SourceId)
                {
                    _treeParents[connection.DestinationId] = "";
                    DeleteNode(connection.DestinationId);
                }

                if (undoable) ActionPerformed?.Invoke(this);
            }
        }

        /// <summary>
        /// Hide a node and all of its children, along with all connections to and from it.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteNode(string id)
        {
            GetNode(id).Render.Visibility = Visibility.Collapsed;

            var connectionsToDelete = ConnectionsContaining(id);
            foreach (var connection in connectionsToDelete)
            {
                DeleteConnection(connection, undoable: false);

                if (connection.DestinationId != id && GetNode(connection.DestinationId).Render.Visibility == Visibility.Visible)
                {
                    DeleteNode(connection.DestinationId);
                }
            }
        }

        public IEnumerable<IVisualPortGraphNode> GetChildren(string nodeId)
        {
            var childIds = ConnectionsContaining(nodeId, asSource: true).Select(conn => conn.DestinationId);
            return childIds.Select(id => _nodesById[id]).Where(node => node.Render.Visibility == Visibility.Visible);
        }

        public void SelectNode(string id, bool multiSelect = false)
        {
            if (multiSelect)
            {
                _selectedNodeIds.Add(id);
            }
            else
            {
                DeselectAllNodes();
            }

            _selectedNodeId = id;
            GetNode(_selectedNodeId)?.Select();
        }

        public IVisualPortGraphNode GetSelectedNode() => !string.IsNullOrEmpty(_selectedNodeId) ? GetNode(_selectedNodeId) : null;
        public HashSet<string> GetSelectedNodeIds() => _selectedNodeIds;

        public void DeselectNode(string id)
        {
            if (id == _selectedNodeId) _selectedNodeId = "";
            if (_selectedNodeIds.Contains(id)) _selectedNodeIds.Remove(id);

            GetNode(id)?.Deselect();
        }

        public void DeselectAllNodes()
        {
            foreach (var id in GetNodeIds())
            {
                DeselectNode(id);
            }

            _selectedNodeIds.Clear();
        }

        public Port GetSelectedPort() => GetNode(_selectedNodeId)?.SelectedPort;

        public void SelectConnection(string id, bool multiSelect = false, bool selectConnectionUI = true)
        {
            _selectedConnectionId = id;
            if (multiSelect) _selectedConnectionIds.Add(id);
            if (selectConnectionUI) GetConnection(id).Select();
        }
        public string GetSelectedConnectionId() => _selectedConnectionId;

        public IPortConnection GetSelectedConnection() => GetConnection(GetSelectedConnectionId());

        public void DeselectConnection(string id)
        {
            if (_selectedConnectionIds.Contains(id)) _selectedConnectionIds.Remove(id);
            _selectedConnectionId = "";
        }

        public void DeselectAllConnections() => _selectedConnectionIds.Clear();

        public void ApplyToAllNodes(Action<IVisualPortGraphNode> lambda)
        {
            var nodes = GetNodes();
            foreach (var node in nodes)
            {
                lambda(node);
            }
        }

        public JToken ToJToken(IEnumerable<IPortConnection> portConnections)
        {
            JArray edgesJArray = new JArray();

            foreach (var connection in portConnections)
            {
                edgesJArray.Add(JObject.Parse(connection.Serialise()));
            }

            return edgesJArray;
        }

        public void SetTreeParent(string childId, string newTreeParentId)
        {
            if (string.IsNullOrEmpty(childId) || string.IsNullOrEmpty(newTreeParentId)) return;

            if (Contains(GetNode(childId)) && Contains(GetNode(newTreeParentId))) _treeParents[childId] = newTreeParentId;
        }

        public string Serialise()
        {
            JObject obj = new JObject();

            // Save nodes
            JArray nodesArray = new JArray();
            var nodes = GetNodes().Where(n => n.Render.Visibility == Visibility.Visible);
            foreach (var node in nodes)
            {
                // nodesArray.Add(new JValue(node.Serialise()));
                nodesArray.Add(JObject.Parse(node.Serialise()));
            }

            obj["Nodes"] = nodesArray;
            obj["NodeIds"] = new JArray(nodes.Select(n => n.Id));

            // Save connections
            obj["Connections"] = ToJToken(GetConnections());

            var generated = obj.ToString();

            return generated;
        }

        public void Deserialise(string memento)
        {
            JObject obj = JObject.Parse(memento);

            // Maintain which nodes exist in the new graph. Any nodes that no longer "exist" should be deleted/hidden.
            var newNodes = new HashSet<string>();

            foreach (var nodeObj in obj["Nodes"])
            {
                var node = GetNode(nodeObj["Id"].ToString());
                node?.Deserialise(nodeObj.ToString());
                newNodes.Add(node.Id);
            }

            var nonExistingNodes = GetNodes().Where(n => !newNodes.Contains(n.Id));

            foreach (var nonExistingNode in nonExistingNodes)
            {
                DeleteNode(nonExistingNode.Id);
            }

            // Maintain which connections exist in the new graph. Any connections that no longer "exist" should be deleted/hidden.
            var newConnections = new HashSet<string>();

            foreach (var cxnObj in obj["Connections"])
            {
                var cxn = GetConnection(cxnObj["Id"].ToString());
                cxn?.Deserialise(cxnObj.ToString());
                newConnections.Add(cxn.Id);
            }

            var nonExistingConnections = GetConnections().Where(c => !newConnections.Contains(c.Id));

            foreach (var nonExistingConnection in nonExistingConnections)
            {
                DeleteConnection(nonExistingConnection, undoable: false);
            }
        }

        // IMemento implementation
        public string Memento { get; private set; }

        public void SaveMemento()
        {
            Memento = Serialise();
        }

        public void LoadMemento(string memento)
        {
            Deserialise(memento);
        }

        public event ActionPerformedDelegate ActionPerformed;

        public string GetTreeParent(string id)
        {
            if (string.IsNullOrEmpty(id) || !_treeParents.ContainsKey(id) || string.IsNullOrEmpty(_treeParents[id]))
            {
                return "";
            }
            else
            {
                return _treeParents[id];
            }
        }

        public void Clear()
        {
            Root = null;

            // foreach (var node in GetNodes())
            // {
            //     DeleteNode(node.Id);
            // }

            _nodesById.Clear();
            _portConnections.Clear();
            _adjLists.Clear();
            _selectedNodeId = null;
            _selectedNodeIds.Clear();
            _treeParents.Clear();
            MainCanvas.Children.Clear();
        }

        public void DeleteSelectedNode()
        {
            DeleteNode(GetSelectedNode().Id);
        }

        public void DeleteSelectedNodes()
        {
            foreach (var nodeId in GetSelectedNodeIds())
            {
                DeleteNode(nodeId);
            }
        }

        public void DeleteSelectedConnection()
        {
            var cxn = GetSelectedConnection();
            if (cxn != null)
            {
                DeleteConnection(cxn, undoable: true);
            }
        }

        public IEnumerable<IPortConnection> ConnectionsContaining(string id, bool asSource = true, bool asDestination = true)
        {
            return GetConnections().Where(conn => (conn.SourceId == id && asSource) || (conn.DestinationId == id && asDestination));
        }

        public IEnumerable<IPortConnection> ConnectionsContaining(IEnumerable<string> ids, bool asSource = true, bool asDestination = true)
        {
            var idSet = new HashSet<string>(ids);

            return GetConnections().Where(conn => (idSet.Contains(conn.SourceId) && asSource) || (idSet.Contains(conn.DestinationId) && asDestination));
        }

        public string GetNodeInformation(string nodeId)
        {
            if (!_nodesById.ContainsKey(nodeId)) return "";
        
            string nodeInfo = GetNode(nodeId).Serialise();
        
            JObject obj = new JObject();
            obj["SelectedNode"] = JObject.Parse(nodeInfo);

            var connections = ConnectionsContaining(nodeId).Where(conn => MainCanvas.Children.Contains(conn.Render));
            var idsUsed = connections.SelectMany(conn => new[] {conn.SourceId, conn.DestinationId}).ToHashSet();

            obj["Connections"] = ToJToken(connections);
            obj["NodeIds"] = new JArray(idsUsed);

            List<string> nodeDumps = new List<string>();

            foreach (var id in idsUsed)
            {
                if (Contains(GetNode(id))) nodeDumps.Add(GetNode(id).Serialise());
            }

            obj["Nodes"] = new JArray(nodeDumps);

            return obj.ToString();
        }

        public string GetNodeSubtree(string rootId)
        {
            if (!_nodesById.ContainsKey(rootId)) return "";

            JObject obj = new JObject();
            obj["SubtreeRoot"] = JObject.Parse(GetNode(rootId).Serialise());

            HashSet<string> subtreeNodes = new HashSet<string>();
            DepthFirstTraversal(rootId, subtreeNodes);

            var connections = ConnectionsContaining(subtreeNodes, asSource: true, asDestination: false);
            obj["Connections"] = ToJToken(connections);
            obj["NodeIds"] = new JArray(subtreeNodes);

            List<string> nodeDumps = new List<string>();

            foreach (var id in subtreeNodes)
            {
                if (Contains(GetNode(id))) nodeDumps.Add(GetNode(id).Serialise());
            }

            obj["Nodes"] = new JArray(nodeDumps);

            return obj.ToString();
        }

        public void DepthFirstTraversal(string nodeId, HashSet<string> visited)
        {
            visited.Add(nodeId);

            if (_adjLists.ContainsKey(nodeId))
            {
                foreach (var childId in _adjLists[nodeId])
                {
                    if (!visited.Contains(childId))
                    {
                        DepthFirstTraversal(childId, visited);
                    }
                } 
            }
        }

        public void ChangePortName(string nodeId, string oldPortName, string newPortName)
        {

        }

        /// <summary>
        /// <para></para>
        /// </summary>
        public VisualPortGraph()
        {

        }
    }
}
