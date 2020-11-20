using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// 
    /// </summary>
    public class DirectoryTree : IUI, IDataFlow<string>
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string FilenameFilter { get; set; } = "*.*";
        public string SelectedPath => _selectedPath;

        public double Height
        {
            get => _treeView.Height;
            set => _treeView.Height = value;
        }

        public double Width
        {
            get => _treeView.Width;
            set => _treeView.Width = value;
        }

        public double MaxHeight
        {
            get => _treeView.MaxHeight;
            set => _treeView.MaxHeight = value;
        }

        public double MaxWidth
        {
            get => _treeView.MaxWidth;
            set => _treeView.MaxWidth = value;
        }

        public double MinHeight
        {
            get => _treeView.MinHeight;
            set => _treeView.MinHeight = value;
        }

        public double MinWidth
        {
            get => _treeView.MinWidth;
            set => _treeView.MinWidth = value;
        }

        public string RootDirectory
        {
            get => _rootDirectory;
            set
            {
                _rootDirectory = value;
                _root = CreateNodeFromDirectory(_rootDirectory);
                _treeView.ItemsSource = new[] {_root};
            }
        }

        public double FontSize
        {
            get => _treeView.FontSize;
            set => _treeView.FontSize = value;
        }

        public string Font
        {
            get => _treeView.FontFamily.ToString();
            set => _treeView.FontFamily = new FontFamily(value);
        }

        // Private fields
        private TreeView _treeView = new TreeView()
        {
            FontSize = 12,
            FontFamily = new FontFamily("Consolas")
        };

        private TreeViewItem _root;
        private string _rootDirectory;
        private string _selectedPath;

        // Ports
        private IDataFlow<string> selectedName;
        private IDataFlow<string> selectedFullPath;

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            if (_root == null)
            {
                _root = new TreeViewItem();
                _root.ItemsSource = CreateTreeViewNodes(new List<string>()
                {
                    string.IsNullOrEmpty(_rootDirectory) ? "Error: No root directory provided" : "Error: No items found in root directory."
                }); 
            }

            _treeView.ItemsSource = new[] {_root};

            return _treeView;
        }

        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => RootDirectory;
            set => RootDirectory = value;
        }

        // Methods
        private IEnumerable<TreeViewNode> CreateTreeViewNodes(IEnumerable<object> sourceItems)
        {
            return sourceItems.Select(item => new TreeViewNode() { Header = item });
        }

        public TreeViewNode CreateNodeFromDirectory(string rootDirectory, TreeViewNode parent = null)
        {
            var root = new DirectoryInfo(rootDirectory);
            var directories = root.GetDirectories("*", SearchOption.TopDirectoryOnly);

            var rootNode = new TreeViewNode()
            {
                Header = root.Name,
                TreeParent = parent
            };

            // Recursively run a DFT to create all child nodes
            var nodes = new List<TreeViewNode>();
            foreach (var directory in directories)
            {
                var directoryNode = CreateNodeFromDirectory(directory.FullName, rootNode);
                nodes.Add(directoryNode);

                var directoryFiles = directory.GetFiles(FilenameFilter, SearchOption.TopDirectoryOnly);
                foreach (var file in directoryFiles)
                {
                    var fileNode = new TreeViewNode()
                    {
                        Header = file.Name,
                        TreeParent = directoryNode
                    };

                    (directoryNode.ItemsSource as List<TreeViewNode>)?.Add(fileNode);
                }
            }

            rootNode.ItemsSource = nodes;

            return rootNode;
        }

        /// <summary>
        /// Backtracks up the tree from the node to find the path leading to that node in terms of each node's Header
        /// </summary>
        /// <param name="node"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        private string GetPathToNode(TreeViewNode node, string separator)
        {
            var sb = new StringBuilder();
            var nodeNames = new List<string>() { };

            var currentNode = node;
            while (currentNode.TreeParent != null && currentNode.TreeParent is TreeViewNode parent)
            {
                nodeNames.Add(parent.Header.ToString());
                currentNode = parent;
            }

            nodeNames.Reverse();
            foreach (var nodeName in nodeNames)
            {
                sb.Append(nodeName + separator);
            }

            sb.Append(node.Header);

            return sb.ToString();
        }

        public DirectoryTree()
        {
            _treeView.SelectedItemChanged += (sender, args) =>
            {
                if (args.NewValue is TreeViewNode node)
                {
                    _selectedPath = GetPathToNode(node, "\\");

                    if (selectedName != null) selectedName.Data = _selectedPath;
                    if (selectedFullPath != null) selectedFullPath.Data = node.Header.ToString();
                }
            };
        }

        /// <summary>
        /// A TreeViewItem that keeps track of its parent node.
        /// </summary>
        public class TreeViewNode : TreeViewItem
        {
            public TreeViewNode TreeParent { get; set; }
        }
    }
}
