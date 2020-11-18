using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public string Filter { get; set; } = "*.*";

        public string RootDirectory
        {
            get => _rootDirectory;
            set
            {
                _rootDirectory = value;
                _root = CreateDirectoryTreeViewItem(_rootDirectory);
            }
        }

        // Private fields
        private TreeView _treeView;
        private TreeViewItem _root;
        private string _rootDirectory;

        // Ports

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            _treeView = new TreeView();

            if (_root == null)
            {
                _root = new TreeViewItem();
                _root.ItemsSource = CreateTreeViewItems(new List<string>()
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
        private IEnumerable<TreeViewItem> CreateTreeViewItems(List<string> sourceItems)
        {
            return sourceItems.Select(s => new TreeViewItem() { Header = s });
        }

        public TreeViewItem CreateDirectoryTreeViewItem(string rootDirectory)
        {
            var root = new DirectoryInfo(rootDirectory);
            var directories = root.GetDirectories(searchPattern: Filter, SearchOption.TopDirectoryOnly);

            var rootTreeItem = new TreeViewItem()
            {
                Header = rootDirectory
            };

            // Recursively run a DFT to create all child TreeViewItems
            var treeItems = new List<TreeViewItem>();
            foreach (var directory in directories)
            {
                var treeItem = CreateDirectoryTreeViewItem(directory.FullName);
                treeItems.Add(treeItem);
            }

            rootTreeItem.ItemsSource = treeItems;

            return rootTreeItem;
        }

        public DirectoryTree()
        {
            RootDirectory = "D:\\Coding\\C#\\Projects\\GALADE";
        }
    }
}
