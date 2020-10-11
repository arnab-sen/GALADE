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
    public class RightTreeLayout<T> : IDataFlow<T>
    {
        // Public fields and properties
        public string InstanceName = "Default";

        public Func<T, string> GetID;               // Required - returns the ID of the node render, used for cycle detection
        public Func<T, double> GetWidth;            // Required - returns the width of the node render
        public Func<T, double> GetHeight;           // Required - returns the height of the node render
        public Action<T, double> SetX;              // Required - sets the x-coordinate of the node render
        public Action<T, double> SetY;              // Required - sets the y-coordinate of the node render
        public Func<T, IEnumerable<T>> GetChildren; // Required - returns a collection of the node's children
        public double HorizontalGap = 100;          // Optional - sets the horizontal distance between every parent and child node render
        public double VerticalGap = 100;            // Optional - sets the vertical distance between node renders in the same layer (the same depth from the root)
        public double InitialX = 0;                 // Optional - sets the x-coordinate of the root node render
        public double InitialY = 0;                 // Optional - sets the y-coordinate of the root node render

        // Private fields
        private HashSet<string> visited = new HashSet<string>();
        private double _maxHeight = 0;

        // Ports

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
                if (!visited.Contains(GetID(child))) return false;
            }

            return true;
        }

        private double SetRightTreeLayout(T node, double horizontalGap, double verticalGap, double x, double y)
        {
            visited.Add(GetID(node));

            double nextX;
            double nextY;

            SetX(node, x);
            SetY(node, y);

            var children = GetChildren(node).ToList();

            var height = GetHeight(node);

            if (IsSaturated(node, children)) // if all of the node's children have been visited
            {
                nextY = y + height + verticalGap;
            }
            else
            {
                nextX = x + GetWidth(node) + horizontalGap;
                nextY = y;

                foreach (var child in children)
                {
                    if (!visited.Contains(GetID(child))) nextY = SetRightTreeLayout(child, horizontalGap, verticalGap, nextX, nextY);
                }
            }

            return Math.Max(nextY, y + height + verticalGap);
        }

        // IDataFlow<T> implementation
        T IDataFlow<T>.Data
        {
            get => default;
            set
            {
                visited.Clear();

                if (ParametersInstantiated()) SetRightTreeLayout(value, HorizontalGap, VerticalGap, InitialX, InitialY);
            }
        }
    }
}
