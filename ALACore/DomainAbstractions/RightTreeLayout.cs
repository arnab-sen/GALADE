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
    /// Runs an algorithm that sets the positions of nodes of type T in a depth-first traversal tree, ensuring that the nodes are laid out from left to right, and that there are no overlaps.
    /// </summary>
    public class RightTreeLayout<T> : IDataFlow<T>, IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Configurations

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

        // Optional - sets the horizontal distance between every parent and child node render
        public double HorizontalGap { get; set; } = 100;    
        
        // Optional - sets the vertical distance between node renders in the same layer (the same depth from the root)
        public double VerticalGap { get; set; } = 100;          
        
        // Optional - sets the x-coordinate of the root node render
        public double InitialX { get; set; } = 0;       
        
        // Optional - sets the y-coordinate of the root node render
        public double InitialY { get; set; } = 0;       
        
        // Optional - gets a set of IDs of nodes not to visit after visiting the initial node
        public Func<HashSet<string>> GetRoots { get; set; }       

        // Outputs for future runs
        public double LatestX => _latestX;
        public double LatestY => _latestY;

        // Private fields
        private HashSet<string> _visited = new HashSet<string>();
        private double _maxHeight = 0;
        private double _latestX = 0;
        private double _latestY = 0;

        // Ports
        private IDataFlow<HashSet<string>> visitedNodes;
        private IEvent complete;

        /// <summary>
        /// Runs an algorithm that sets the positions of nodes of type T in a depth-first traversal tree, ensuring that the nodes are laid out from left to right, and that there are no overlaps.
        /// </summary>
        public RightTreeLayout()
        {

        }

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
                    _latestX = x + GetWidth(node) + horizontalGap;
                    _latestY = y;

                    foreach (var child in children)
                    {
                        if (!_visited.Contains(GetID(child))) _latestY = SetRightTreeLayout(child, horizontalGap, verticalGap, _latestX, _latestY);
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

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => default;
            set
            {
                if (GetRoots != null)
                {
                    var rootIds = GetRoots();
                    foreach (var rootId in rootIds)
                    {
                        _visited.Add(rootId);
                    }
                }

                if (ParametersInstantiated()) SetRightTreeLayout(value, HorizontalGap, VerticalGap, InitialX, InitialY);

                if (visitedNodes != null) visitedNodes.Data = _visited.Select(n => n).ToHashSet();
                complete?.Execute();
            }
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            _visited.Clear();
        }
    }
}
