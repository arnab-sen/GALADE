using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ProgrammingParadigms;
using EnvDTE;
using Expression = EnvDTE.Expression;

namespace DomainAbstractions
{
    public class DebugViewer : IUI, IDataFlow<StackFrame> // child, stackFrame
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Grid _mainGrid = new Grid()
        {
            Background = new SolidColorBrush(Colors.White),
        };

        private StackFrame _currentStackFrame;

        // Ports

        // IUI implementation
        UIElement IUI.GetWPFElement() => _mainGrid;

        // IDataFlow<StackFrame> implementation
        StackFrame IDataFlow<StackFrame>.Data
        {
            get => _currentStackFrame;
            set
            {
                LoadStackFrame(value as StackFrame);
            }
        }

        // Methods
        public Dictionary<string, string> GetVariablesFromStackFrame(StackFrame stackFrame)
        {
            var variablePairs = new Dictionary<string, string>();
            var localVars = stackFrame.Locals;

            Dictionary<string, string> classVariables = new Dictionary<string, string>();

            var localsEnumerator = localVars.GetEnumerator();
            while (localsEnumerator.MoveNext())
            {
                var current = localsEnumerator.Current as Expression;
                if (current == null) continue;

                variablePairs[current.Name] = current.Value;

                if (current.Name == "this") classVariables = GetClassVariables(current);

                if (current.Value.Contains("Count"))
                {

                }
            }

            foreach (var classVariable in classVariables)
            {
                variablePairs[classVariable.Key] = classVariable.Value;
            }

            return variablePairs;
        }

        /// <summary>
        /// Returns all non-local variables found in a class Expression.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetClassVariables(Expression thisVariable)
        {
            var dataMembers = new Dictionary<string, string>();

            var members = thisVariable.DataMembers;
            var membersEnumerator = members.GetEnumerator();
            while (membersEnumerator.MoveNext())
            {
                var currentMember = membersEnumerator.Current as Expression;
                if (currentMember == null) continue;

                dataMembers[currentMember.Name] = currentMember.Value;

                if (currentMember.Value.Contains("Count"))
                {
                    // Check currentMember.Collection and currentMember.DataMembers - should examine when the value is selected rather than combine it with the rest
                }
            }

            return dataMembers;
        }

        private List<T> ToList<T>(IEnumerator enumerator)
        {
            var createdList = new List<T>();
            while (enumerator.MoveNext())
            {
                createdList.Add((T)enumerator.Current);
            }

            return createdList;
        }

        public void LoadStackFrame(StackFrame stackFrame)
        {
            _currentStackFrame = stackFrame;

            var localExpressions = ToList<Expression>(stackFrame.Locals.GetEnumerator());

            var thisVar = ToList<Expression>(stackFrame.Locals.GetEnumerator()).First(expr => expr.Name == "this");
            var dataMemberExpressions = new List<Expression>();
            dataMemberExpressions.AddRange(ToList<Expression>(thisVar.DataMembers.GetEnumerator()));

            _mainGrid.RowDefinitions.Clear();

            // Add column labels
            _mainGrid.RowDefinitions.Add(new RowDefinition());

            var nameColumnLabel = new Label()
            {
                Content = "Name",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(nameColumnLabel, 0);
            Grid.SetColumn(nameColumnLabel, 0);
            _mainGrid.Children.Add(nameColumnLabel);

            var valueColumnLabel = new Label()
            {
                Content = "Value",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(valueColumnLabel, 0);
            Grid.SetColumn(valueColumnLabel, 1);
            _mainGrid.Children.Add(valueColumnLabel);

            // Add rows
            foreach (var localExpression in localExpressions)
            {
                _mainGrid.RowDefinitions.Add(new RowDefinition());

                var nameText = new TextBlock()
                {
                    Text = localExpression.Name,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(nameText, _mainGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(nameText, 0);
                _mainGrid.Children.Add(nameText);

                var valueText = new TextBlock()
                {
                    Text = localExpression.Value,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(valueText, _mainGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(valueText, 1);
                _mainGrid.Children.Add(valueText);
            }

        }


        public DebugViewer()
        {
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

    }
}
