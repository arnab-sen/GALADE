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

        /// <summary>
        /// Only include StackFrames that meet a given condition. The input object should be cast as a StackFrame.
        /// </summary>
        public Func<object, bool> Filter { get; set; }

        // Private fields
        private ListView _mainContainer = new ListView() {};
        private ObservableCollection<object> _stackFrames = new ObservableCollection<object>();
        private List<object> _callStack;
        private StackFrame _latestStackFrame;

        // Ports
        private IDataFlow<StackFrame> selectedStackFrame;
        private IDataFlow<string> selectedLabel;

        // IUI implementation
        UIElement IUI.GetWPFElement() => _mainContainer;

        // IDataFlow<List<object>> implementation
        List<object> IDataFlow<List<object>>.Data
        {
            get => _callStack;
            set
            {
                if (Filter == null)
                {
                    _callStack = value; 
                }
                else
                {
                    _callStack = value.Where(Filter).ToList();
                }

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

            mainView.UpdateLayout();
        }

        public Grid CreateStackFrameGrid(StackFrame stackFrame)
        {
            var grid = new Grid()
            {
                Background = Brushes.Transparent
            };

            ScrollViewer.SetCanContentScroll(grid, true);

            grid.RowDefinitions.Clear();

            // Local variables section
            var localVarTitle = new Label()
            {
                Content = "Local Variables:",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.AddRow(localVarTitle);

            // Add column labels
            grid.AddRow(
                CreateCellBorder("Name", new Thickness(1, 1, 1, 1)), 
                CreateCellBorder("Type", new Thickness(0, 1, 1, 1)),
                CreateCellBorder("Value", new Thickness(0, 1, 1, 1)));

            // Add rows
            AddExpressionPairs(grid, stackFrame.GetAllLocalVariables());

            // Non-local variables
            var nonLocalVarTitle = new Label()
            {
                Content = "Non-local Variables:",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.AddRow(nonLocalVarTitle);

            // Add column labels
            grid.AddRow(
                CreateCellBorder("Name", new Thickness(1, 1, 1, 1)),
                CreateCellBorder("Type", new Thickness(0, 1, 1, 1)),
                CreateCellBorder("Value", new Thickness(0, 1, 1, 1)));

            // Add rows
            AddExpressionPairs(grid, stackFrame.GetAllNonLocalVariables());

            return grid;
        }

        private void AddExpressionPairs(Grid grid, List<Expression> expressions)
        {
            for (int i = 0; i < expressions.Count; i++)
            {
                var expression = expressions[i];
                if (string.IsNullOrEmpty(expression.Name) || string.IsNullOrEmpty(expression.Type) || string.IsNullOrEmpty(expression.Value)) continue;

                grid.AddRow(
                    CreateCellBorder(FormatDottedName(expression.Name), new Thickness(1, 0, 1, 1)),
                    CreateCellBorder(FormatDottedName(expression.Type), new Thickness(0, 0, 1, 1)),
                    CreateCellBorder(expression.Value, new Thickness(0, 0, 1, 1)));
            }
        }

        private Border CreateCellBorder(string textContent, Thickness borderThickness)
        {
            var border = new Border()
            {
                BorderBrush = Brushes.Black,
                BorderThickness = borderThickness
            };

            var textBlock = new TextBlock()
            {
                Text = textContent,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 200,
                Margin = new Thickness(1)
            };

            border.Child = textBlock;

            return border;
        }

        private string FormatDottedName(string unformatted)
        {
            var sb = new StringBuilder();
            var split = unformatted.Split('.');
            sb.AppendLine(split.FirstOrDefault());
            foreach (var s in split.Skip(1))
            {
                sb.AppendLine($"    .{s}");
            }

            return sb.ToString();
        }

        private StackPanel CreateNode(StackFrame stackFrame)
        {
            var vertPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                CanVerticallyScroll = true,
                MinHeight = 50,
                MaxWidth = 400,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var horizPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            vertPanel.Children.Add(horizPanel);

            var nameTextBlock = new TextBlock()
            {
                Text = FormatDottedName(stackFrame.FunctionName),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 300
            };

            if (stackFrame.Locals.Count > 50)
            {
                nameTextBlock.Text = $"[Warning: Many items detected]\n{nameTextBlock.Text}";
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

            var expandButtonContainer = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            expandButtonContainer.Children.Add(expandButton);

            horizPanel.Children.Add(expandButtonContainer);
            horizPanel.Children.Add(nameTextBlock);

            expandButton.Click += (sender, args) =>
            {
                if (stackFrame.Locals.Count > 50)
                {
                    var msgBoxText = "Warning: Many items detected. Expanding this section may take a long time. Continue?";
                    var msgBoxButton = MessageBoxButton.YesNo;
                    var msgBoxCaption = "Continue?";
                    var msgBoxIcon = MessageBoxImage.Exclamation;

                    var msgBoxResult = MessageBox.Show(msgBoxText, msgBoxCaption, msgBoxButton, msgBoxIcon);
                    if (msgBoxResult == MessageBoxResult.No) return;
                }

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

            vertPanel.MouseLeftButtonUp += (sender, args) =>
            {
                if (selectedStackFrame != null) selectedStackFrame.Data = stackFrame;
            };

            return vertPanel;
        }


        public DebugViewer()
        {
            ScrollViewer.SetCanContentScroll(_mainContainer, false);
        }

    }
}
