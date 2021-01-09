using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Runs an algorithm that sets the positions of nodes of type T in a graph, ensuring that the nodes are laid out from left to right, and that there are no overlaps.</para>
    /// <para>The following properties must be defined for this abstraction to function correctly: GetID, GetWidth, GetHeight, SetX, SetY, GetChildren, and Roots.</para>
    /// </summary>
    public class RightTreeLayout<T> : IEvent // start
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Required - returns the ID of the node render, and is used for cycle detection
        public Func<T, string> GetID { get; set; }
        
        // Required - returns the width of the node render
        public Func<T, double> GetWidth { get; set; }
        
        // Required - returns the height of the node render
        public Func<T, double> GetHeight { get; set; }    
        
        // Required - sets the x-coordinate of the node render
        public Action<T, double> SetX { get; set; }   
        
        // Required - sets the y-coordinate of the node render
        public Action<T, double> SetY { get; set; }    
        
        // Required - returns a collection of the node's children
        public Func<T, IEnumerable<T>> GetChildren { get; set; }  

        // Required - a list of roots to traverse through
        public List<T> Roots { get; set; }

        // Optional - a list that contains every node in the graph. This will be used to try and lay out any nodes that weren't laid out in the first pass.
        public List<T> AllNodes { get; set; }

        // Optional - sets the horizontal distance between every parent and child node render
        public double HorizontalGap { get; set; } = 100;    
        
        // Optional - sets the vertical distance between node renders in the same layer (the same depth from the root)
        public double VerticalGap { get; set; } = 100;          
        
        // Optional - sets the x-coordinate of the root node render
        public double InitialX { get; set; } = 0;       
        
        // Optional - sets the y-coordinate of the root node render
        public double InitialY { get; set; } = 0;       
        
        /// <summary>
        /// <para>Optional - contains the tree parent for each node ID. This will override the default behaviour of automatically finding tree parents through a BFS or DFS. </para>
        /// <para>Useful for when you want to maintain a consistent graph topology.</para>
        /// </summary>
        public Dictionary<string, T> TreeParents { get; set; }

        // Only the latest y-coordinate needs to be known globally
        public double LatestY => _latestY;

        // Private fields
        private HashSet<string> _visited = new HashSet<string>();
        private List<T> _roots = new List<T>();
        private double _latestY = 0;
        private Dictionary<string, List<T>> _treeConnections = new Dictionary<string, List<T>>();
        private Func<T, IEnumerable<T>> _userDefinedGetChildren;

        // Ports
        private IEvent complete;

        private bool ParametersInstantiated()
        {
            var requiredParameters = new List<object>(){ GetWidth, GetHeight, SetX, SetY, GetChildren };

            foreach (object requiredParameter in requiredParameters)
            {
                if (requiredParameter == null) return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the node has any children that haven't been visited yet.
        /// Leaf nodes are saturated by default.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        private bool IsSaturated(T node, IEnumerable<T> children)
        {
            foreach (var child in children)
            {
                if (!_visited.Contains(GetID(child))) return false;
            }

            return true;
        }

        private double SetRightTreeLayout(T node, double horizontalGap, double verticalGap, double x, double y)
        {
            try
            {
                _visited.Add(GetID(node));

                SetX(node, x);
                SetY(node, y);

                var children = GetChildren(node).ToList();

                var height = GetHeight(node);

                if (IsSaturated(node, children)) // if all of the node's children have been visited
                {
                    _latestY = y + height + verticalGap;
                }
                else
                {
                    var latestX = x + GetWidth(node) + horizontalGap;
                    _latestY = y;

                    foreach (var child in children)
                    {
                        if (!_visited.Contains(GetID(child))) _latestY = SetRightTreeLayout(child, horizontalGap, verticalGap, latestX, _latestY);
                    }
                }

                _latestY = Math.Max(_latestY, y + height + verticalGap);
                return _latestY;
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to lay out nodes in RightTreeLayout when visiting {node}.\nException: {e}");

                _latestY = y;
                return _latestY;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        private void CreateBreadthFirstTree(T root)
        {
            _treeConnections.Clear();

            var q = new Queue<T>();
            q.Enqueue(root);
            var treeParentFound = new HashSet<string>();

            while (q.Any())
            {
                var parent = q.Dequeue();
                var parentId = GetID(parent);

                if (!_treeConnections.ContainsKey(parentId))
                {
                    var children = GetChildren(parent);
                    _treeConnections[parentId] = new List<T>();

                    foreach (var child in children)
                    {
                        var childId = GetID(child);

                        if (!treeParentFound.Contains(childId))
                        {
                            _treeConnections[parentId].Add(child);
                            treeParentFound.Add(childId);
                            q.Enqueue(child);
                        }
                    } 
                }
            }
        }

        private void Start()
        {
            _visited.Clear();

            if (ParametersInstantiated())
            {
                if (_userDefinedGetChildren == null)
                {
                    _userDefinedGetChildren = GetChildren;
                }

                // Overwrite GetChildren to only retrieve children that the node is the TreeParent of
                GetChildren = node =>
                {
                    var allChildren = _userDefinedGetChildren(node);
                    var treeChildren = allChildren.Where(c => !TreeParents.ContainsKey(GetID(c)) || TreeParents[GetID(c)].Equals(node));

                    return treeChildren;
                };

                _latestY = InitialY;

                // Do a layout traversal pass from every root
                foreach (var root in Roots)
                {
                    SetRightTreeLayout(root, HorizontalGap, VerticalGap, InitialX, _latestY);
                }

                // Do a layout traversal pass from every node that hasn't been visited
                if (AllNodes != null)
                {
                    foreach (var node in AllNodes)
                    {
                        if (!_visited.Contains(GetID(node)))
                        {
                            SetRightTreeLayout(node, HorizontalGap, VerticalGap, InitialX, _latestY);
                        }
                    }
                }
            }

            complete?.Execute();
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Start();
        }

        public RightTreeLayout()
        {

        }
    }
}
