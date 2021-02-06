using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using EnvDTE;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Expression = EnvDTE.Expression;

namespace DomainAbstractions
{
    public class DebugViewer : IUI, IDataFlow<List<object>>, IDataFlow<object> // child, stackFrames, addStackFrame
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private ListView _mainContainer = new ListView() {};
        private ObservableCollection<object> _stackFrames = new ObservableCollection<object>();
        private List<object> _callStack;
        private StackFrame _latestStackFrame;

        // Ports
        private IDataFlow<StackFrame> selectedStackFrame;

        // IUI implementation
        UIElement IUI.GetWPFElement() => _mainContainer;

        // IDataFlow<List<object>> implementation
        List<object> IDataFlow<List<object>>.Data
        {
            get => _callStack;
            set
            {
                _callStack = value;
                _stackFrames.Clear();
                UpdateStackFrameViewer(_mainContainer, _callStack, _stackFrames);

            }
        }

        // IDataFlow<StackFrame> implementation
        object IDataFlow<object>.Data
        {
            get => _latestStackFrame;
            set
            {
                _latestStackFrame = value as StackFrame;
                _stackFrames.Add(CreateNode(_latestStackFrame));
            }
        }

        // Methods
        public void UpdateStackFrameViewer(ListView mainView, List<object> callStack, ObservableCollection<object> itemCollection)
        {
            mainView.ItemsSource = itemCollection;
            foreach (StackFrame stackFrame in callStack)
            {
                itemCollection.Add(CreateNode(stackFrame));
            }

        }

        public Grid CreateStackFrameGrid(StackFrame stackFrame)
        {
            var grid = new Grid()
            {
                Background = Brushes.White
            };

            ScrollViewer.SetCanContentScroll(grid, true);

            grid.RowDefinitions.Clear();

            // Local variables section
            var localVarTitle = new Label()
            {
                Content = "Local Variables",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.AddRow(localVarTitle);

            // Add column labels
            var nameColumnLabel = new Label()
            {
                Content = "Name",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var valueColumnLabel = new Label()
            {
                Content = "Value",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            grid.AddRow(nameColumnLabel, valueColumnLabel);

            // Add rows
            AddExpressionPairs(grid, stackFrame.GetAllLocalVariables());

            // Non-local variables
            var nonLocalVarTitle = new Label()
            {
                Content = "Non-local Variables",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.AddRow(nonLocalVarTitle);

            // Add rows
            AddExpressionPairs(grid, stackFrame.GetAllNonLocalVariables());

            return grid;
        }

        private void AddExpressionPairs(Grid grid, List<Expression> expressions)
        {
            foreach (var expression in expressions)
            {
                var nameText = new TextBlock()
                {
                    Text = expression.Name,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                var valueText = new TextBlock()
                {
                    Text = expression.Value,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                grid.AddRow(nameText, valueText);
            }
        }

        private StackPanel CreateNode(StackFrame stackFrame)
        {
            var vertPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                CanVerticallyScroll = true,
                MinHeight = 50
            };

            var horizPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };

            vertPanel.Children.Add(horizPanel);

            var nameTextBlock = new TextBlock()
            {
                Text = stackFrame.FunctionName,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500
            };

            if (stackFrame.Locals.Count > 50)
            {
                nameTextBlock.Text = $"[Warning: Many items detected] {nameTextBlock.Text}";
            } 

            var expandButtonContent = new Label()
            {
                Content = "Expand",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var expandButton = new System.Windows.Controls.Button()
            {
                Content = expandButtonContent,
                Height = 30,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            horizPanel.Children.Add(nameTextBlock);
            horizPanel.Children.Add(expandButton);

            expandButton.Click += (sender, args) =>
            {
                if (vertPanel.Children.Count == 1)
                {
                    vertPanel.Children.Add(CreateStackFrameGrid(stackFrame));
                    expandButtonContent.Content = "Collapse";
                }
                else if (vertPanel.Children.Count == 2)
                {
                    vertPanel.Children.RemoveAt(1);
                    expandButtonContent.Content = "Expand";
                }

            };

            vertPanel.MouseLeftButtonDown += (sender, args) =>
            {
                if (selectedStackFrame != null) selectedStackFrame.Data = stackFrame;
            };

            return vertPanel;
        }


        public DebugViewer()
        {

        }

    }
}
