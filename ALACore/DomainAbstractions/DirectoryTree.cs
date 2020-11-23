using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private IEnumerable<TreeViewItem> CreateTreeViewNodes(IEnumerable<object> sourceItems)
        {
            return sourceItems.Select(item => new TreeViewItem() { Header = item });
        }

        public TreeViewItem CreateNodeFromDirectory(string rootDirectory, TreeViewItem parent = null)
        {
            var root = new DirectoryInfo(rootDirectory);
            var directories = root.GetDirectories("*", SearchOption.TopDirectoryOnly);

            var rootNode = new TreeViewItem()
            {
                Header = root.Name,
                Tag = parent
            };

            // Recursively run a DFT to create all child nodes
            var nodes = new ObservableCollection<TreeViewItem>();
            foreach (var directory in directories)
            {
                var directoryNode = CreateNodeFromDirectory(directory.FullName, rootNode);
                nodes.Add(directoryNode);

                var directoryFiles = directory.GetFiles(FilenameFilter, SearchOption.TopDirectoryOnly);
                foreach (var file in directoryFiles)
                {
                    var fileNode = new TreeViewItem()
                    {
                        Header = file.Name,
                        Tag = directoryNode
                    };

                    (directoryNode.ItemsSource as ObservableCollection<TreeViewItem>)?.Add(fileNode);
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
        private string GetPathToNode(TreeViewItem node, string separator)
        {
            var sb = new StringBuilder();
            var nodeNames = new List<string>() { };

            var currentNode = node;
            while (currentNode.Tag != null && currentNode.Tag is TreeViewItem parent)
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
                if (args.NewValue is TreeViewItem node)
                {
                    _selectedPath = GetPathToNode(node, "\\");

                    if (selectedName != null) selectedName.Data = _selectedPath;
                    if (selectedFullPath != null) selectedFullPath.Data = node.Header.ToString();
                }
            };
        }

    }
}
