using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ProgrammingParadigms;
using EnvDTE;
using EnvDTE80;
using Libraries;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace DomainAbstractions
{
    /// <summary>
    /// A class that hooks onto and interacts with the debugger of the first opened instance of Visual Studio 2019.
    /// </summary>
    public class VSDebugger : IEvent // connect
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public DTE2 CurrentDTE => _dte;

        // Private fields
        private DTE2 _dte;
        private Debugger _debugger;
        private DebuggerEvents _debuggerEvents; // Reference to this must be kept alive in order to use the events, so we define it here
        private DTEEvents _dteEvents;

        // Ports
        private IDataFlow<List<object>> currentCallStack;

        // IEvent implementation
        void IEvent.Execute() => ConnectToVisualStudio(getUserSelection: true);

        // Methods
        /// <summary>
        /// First, all instances of Visual Studio are found. Then, if getUserSelection is true, the user will be
        /// able to choose which instance to connect to, otherwise the first instance found will be connected to.
        /// </summary>
        /// <param name="VSVersion"></param>
        public void ConnectToVisualStudio(bool getUserSelection = false)
        {
            var runningObjects = GetRunningObjects(regex: @"VisualStudio.DTE").OfType<DTE2>().ToList();

            if (runningObjects.Count == 0)
            {
                Logging.Log($"VSDebugger: No instances of Visual Studio found... failed to connect.");
                return;
            }

            if (getUserSelection)
            {
                PresentVSChoice(runningObjects);
            }
            else
            {
                var firstNonActiveInstance = runningObjects.FirstOrDefault(obj => !obj.MainWindow.Caption.Contains("(Running) - Microsoft Visual Studio")) ?? runningObjects.First();
                InitDTE(firstNonActiveInstance);
            }
        }

        private void InitDTE(DTE2 dte)
        {
            _dte = dte;
            _debugger = dte.Debugger;
            _debuggerEvents = dte.Events.DebuggerEvents;

            Logging.Message($"Connected to Visual Studio Instance \"{dte.MainWindow.Caption}\"");

            // _debuggerEvents.OnEnterBreakMode += (dbgEventReason reason, ref dbgExecutionAction action) =>
            // {
            //
            // };
            //
            // _dteEvents = dte.Events.DTEEvents;
            // _dteEvents.ModeChanged += mode =>
            // {
            //     if (mode == vsIDEMode.vsIDEModeDebug)
            //     {
            //
            //     }
            // };
        }

        private void PresentVSChoice(List<DTE2> candidates)
        {
            if (candidates.Count == 0) return;

            var selectionWindow = new System.Windows.Window()
            {
                Topmost = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "GALADE"
            };

            var descriptors = new List<string>();
            var sb = new StringBuilder();
            foreach (var dte in candidates)
            {
                sb.Clear();
                sb.Append(dte.MainWindow?.Caption);

                if (string.IsNullOrEmpty(dte.ActiveDocument?.FullName))
                {
                    sb.Append(" (No document open)");
                }
                else
                {
                    sb.Append(dte.ActiveDocument?.FullName);
                }

                descriptors.Add(sb.ToString());
            }

            var mainPanel = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5)
            };

            selectionWindow.Content = mainPanel;

            var horizPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            mainPanel.Children.Add(new TextBlock()
            {
                Text = "Multiple instances of Visual Studio detected.\nPlease select one from the dropdown below, which shows the window name and currently opened document path in each instance:",
                Margin = new Thickness(1)
            });

            mainPanel.Children.Add(horizPanel);

            var dropDown = new ComboBox()
            {
                ItemsSource = descriptors.ToList(),
                // Width = 250,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 25,
                IsEditable = true,
                SelectedIndex = 0,
                Text = descriptors.First(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(1)
            };

            var okButton = new System.Windows.Controls.Button()
            {
                Content = "OK",
                Width = 50,
                Height = 25,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(1)
            };

            okButton.Click += (sender, args) =>
            {
                selectionWindow.Close();
                InitDTE(candidates[dropDown.SelectedIndex]);
            };

            horizPanel.Children.Add(dropDown);
            horizPanel.Children.Add(okButton);

            selectionWindow.Show();
        }

        /// <summary>
        /// Sets and enables a breakpoint at the given line number of the file found at the given file path.
        /// If the line number is not a valid breakpoint line, and the line contains a method signature, then the next valid line number will be used.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        public void AddBreakpoint(string filePath, int lineNumber, int column = 1, string condition = "")
        {
            if (_dte == null) ConnectToVisualStudio();

            if (string.IsNullOrEmpty(condition))
            {
                _debugger?.Breakpoints.Add(File: filePath, Line: lineNumber, Column: column);
            }
            else
            {

                var existingBreakpoint = GetBreakpoint(filePath, lineNumber);
                if (existingBreakpoint == null)
                {
                    _debugger?.Breakpoints.Add(File: filePath, Line: lineNumber, Column: column, Condition: condition, ConditionType: dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue);
                }
                else
                {
                    var newCondition = $"{existingBreakpoint.Condition} || {condition}";
                    existingBreakpoint.Delete();
                    _debugger?.Breakpoints.Add(File: filePath, Line: lineNumber, Column: column, Condition: newCondition, ConditionType: dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue);
                }
            }
            
        }

        /// <summary>
        /// Returns the first breakpoint found at the given filepath, line number, and column. If none are found, null is returned.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        /// <param name="column">The character position of the breakpoint. Ignored if -1, and the default value is -1.</param>
        /// <returns></returns>
        public Breakpoint GetBreakpoint(string filePath, int lineNumber, int column = -1)
        {
            if (_dte == null) ConnectToVisualStudio();

            if (_debugger != null)
            {
                var enumerator = _debugger.Breakpoints.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is Breakpoint breakpoint)
                    {
                        if (breakpoint.File == filePath && breakpoint.FileLine == lineNumber && (column == -1 || breakpoint.FileColumn == column)) return breakpoint;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Deletes all breakpoints in the file at the given filepath.
        /// </summary>
        /// <param name="filePath"></param>
        public void ClearAllBreakpoints(string filePath)
        {
            if (_dte == null) ConnectToVisualStudio();

            if (_debugger != null)
            {
                var enumerator = _debugger.Breakpoints.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is Breakpoint breakpoint)
                    {
                        if (breakpoint.File == filePath) breakpoint.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// Returns a List of EnvDTE.StackFrame objects representing the current call stack. They are cast as objects due to compatibility issues across assemblies for interop types.
        /// The first element in the list is the most recent EnvDTE.StackFrame.
        /// The list of StackFrames is also sent through the currentCallStack output port.
        /// </summary>
        /// <returns></returns>
        public List<object> GetAndSendCurrentCallStack()
        {
            if (_dte == null) ConnectToVisualStudio();

            var callStack = _debugger?.CurrentThread?.StackFrames.GetEnumerator().ToList<object>() ?? new List<object>();

            if (currentCallStack != null) currentCallStack.Data = callStack;

            return callStack;
        }

        public void Continue()
        {
            if (_dte == null) ConnectToVisualStudio();

            _debugger?.Go(WaitForBreakOrEnd: false);
        }

        public void StepOver()
        {
            if (_dte == null) ConnectToVisualStudio();

            _debugger?.StepOver(WaitForBreakOrEnd: false);
        }

        public void StepInto()
        {
            if (_dte == null) ConnectToVisualStudio();

            _debugger?.StepInto(WaitForBreakOrEnd: false);
        }

        public void StepOut()
        {
            if (_dte == null) ConnectToVisualStudio();

            _debugger?.StepOut(WaitForBreakOrEnd: false);
        }

        public void Stop()
        {
            if (_dte == null) ConnectToVisualStudio();

            _debugger?.Stop(WaitForDesignMode: false);
        }

        public void ExecuteStatement(string statement)
        {
            if (_dte == null) ConnectToVisualStudio();

            _debugger?.ExecuteStatement(statement);
        }

        /// <summary>
        /// Based on a snippet from https://docs.microsoft.com/en-us/visualstudio/extensibility/launch-visual-studio-dte?view=vs-2019.
        /// </summary>
        private static List<object> GetRunningObjects(string regex)
        {
            List<object> runningObjects = new List<object>();

            try
            {
                IBindCtx bindContext = null;
                Utilities.CreateBindCtx(0, out bindContext);

                IRunningObjectTable runningObjectTable = null;
                bindContext.GetRunningObjectTable(out runningObjectTable);

                IEnumMoniker monikerEnumerator = null;
                runningObjectTable.EnumRunning(out monikerEnumerator);

                object runningObject = null;
                IMoniker[] monikers = new IMoniker[1];
                IntPtr numberFetched = IntPtr.Zero;
                while (monikerEnumerator.Next(1, monikers, numberFetched) == 0)
                {
                    IMoniker moniker = monikers[0];

                    string objectDisplayName = null;
                    try
                    {
                        moniker.GetDisplayName(bindContext, null, out objectDisplayName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Some ROT objects require elevated permissions.
                    }

                    if (!string.IsNullOrWhiteSpace(objectDisplayName))
                    {
                        if (Regex.IsMatch(objectDisplayName, regex))
                        {
                            runningObjectTable.GetObject(moniker, out runningObject);
                            if (runningObject != null) runningObjects.Add(runningObject);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log($"Exception thrown in VSDebugger.GetRunningObjects:\n{e}");
                return runningObjects;
            }

            return runningObjects;
        }

        public VSDebugger()
        {

        }
    }
}
