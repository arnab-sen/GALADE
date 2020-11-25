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
            var treeViewItems = sourceItems.Select(item => new TreeViewItem() {Header = item});

            foreach (var treeViewItem in treeViewItems)
            {
                SubscribeEvents(treeViewItem);
            }

            return treeViewItems;
        }

        public TreeViewItem CreateNodeFromDirectory(string rootDirectory, TreeViewItem parent = null)
        {
            var root = new DirectoryInfo(rootDirectory);
            var directories = root.GetDirectories("*", SearchOption.TopDirectoryOnly);

            var rootNode = new TreeViewItem()
            {
                Header = parent != null ? root.Name : root.FullName,
                Tag = parent
            };

            SubscribeEvents(rootNode);

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

                    SubscribeEvents(fileNode);

                    (directoryNode.ItemsSource as ObservableCollection<TreeViewItem>)?.Add(fileNode);
                }
            }

            rootNode.ItemsSource = nodes;

            return rootNode;
        }

        private void SubscribeEvents(TreeViewItem item)
        {
            item.PreviewMouseRightButtonDown += (sender, args) =>
            {
                if (IsDesiredNode(sender, args.OriginalSource))
                {
                    item.IsSelected = true;
                    Output(item);
                    args.Handled = true;
                }
            };

            item.PreviewMouseLeftButtonDown += (sender, args) =>
            {
                if (IsDesiredNode(sender, args.OriginalSource))
                {
                    item.IsSelected = true;
                    Output(item);
                    args.Handled = true;
                }
            };
        }

        private bool IsDesiredNode(object sender, object originalSource)
        {
            return originalSource is FrameworkElement fe
                   && sender is TreeViewItem tvi
                   && fe.DataContext != null
                   && fe.DataContext.ToString() == tvi.Header.ToString();
        }

        private void Output(TreeViewItem item)
        {
            var pathToNode = GetPathToNode(item, "\\");
            _selectedPath = (pathToNode != _rootDirectory) ?  Path.Combine(RootDirectory, pathToNode) : pathToNode;

            if (selectedName != null) selectedName.Data = item.Header.ToString();
            if (selectedFullPath != null) selectedFullPath.Data = _selectedPath;
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

        }

    }
}
