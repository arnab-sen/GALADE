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

namespace DomainAbstractions
{
    /// <summary>
    /// A class that hooks onto and interacts with the debugger of the first opened instance of Visual Studio 2019.
    /// </summary>
    public class VSDebugger : IEvent // connect
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Debugger _debugger;

        // Reference to this must be kept alive in order to use the events, so we define it here
        private DebuggerEvents _debuggerEvents; 
        private Tuple<string, int> _lastPair;
        private Dictionary<string, string> _mappingVSToDTEVersion = new Dictionary<string, string>()
        {
            {"2019", "16"},
            {"2017", "15"},
            {"2015", "14"},
            {"2013", "12"},
            {"2012", "11"},
            {"2010", "10"},
        };

        // Ports

        // IEvent implementation
        void IEvent.Execute() => ConnectToVisualStudio();

        // Methods
        /// <summary>
        /// Creates a hook to the debugger of the first opened VS process of a given version.
        /// </summary>
        /// <param name="VSVersion"></param>
        public void ConnectToVisualStudio(string VSVersion = "2019")
        {
            if (!_mappingVSToDTEVersion.ContainsKey(VSVersion))
            {
                throw new ArgumentException($"Invalid Visual Studio year provided: {VSVersion}. Must be one of: [2019, 2017, 2015, 2013, 2012, 2010].");
            }

            DTE2 dte = (DTE2)Marshal.GetActiveObject($"VisualStudio.DTE.{_mappingVSToDTEVersion[VSVersion]}.0");
            _debugger = dte.Debugger;
            _debuggerEvents = dte.Events.DebuggerEvents;
        }

        /// <summary>
        /// Sets and enables a breakpoint at the given line number of the file found at the given file path.
        /// If the line number is not a valid breakpoint line, and the line contains a method signature, then the next valid line number will be used.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        public void AddBreakpoint(string filePath, int lineNumber, int column = 1, string condition = "")
        {
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

        public void ClearAllBreakpoints(string filePath)
        {
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

        private async void Test()
        {
            // 16 for 2019, 15 for 2017, 14 for 2015, 12 for 2013, 11 for 2012, 10 for 2010
            DTE2 dte = (DTE2)Marshal.GetActiveObject("VisualStudio.DTE.16.0");

            try
            {
                _debugger = dte.Debugger;
                _debuggerEvents = dte.Events.DebuggerEvents;
                // _debuggerEvents.OnEnterBreakMode += (dbgEventReason reason, ref dbgExecutionAction action) =>
                // {
                //     var a = _debugger.CurrentStackFrame?.Locals.ToString() ?? "none found";
                //
                //     Logging.Log("----- Testing -----");
                //     Logging.Log(a);
                //     Logging.Log("----- End Testing -----");
                //
                // };

                while (true)
                {
                    // var functionName = "ProgrammingParadigms.EventConnector.ProgrammingParadigms.IEvent.Execute()";
                    // _debugger.Breakpoints.Add(Function: functionName);
                    // _debugger.Breakpoints.Add(Function: "Application.Application.InitTest()");
                    // _debugger.Breakpoints.Add(Function: "Application.Application.CreateWiring()");
                    // _debugger.Breakpoints.Add(Function: "ProgrammingParadigms.EventConnector.ProgrammingParadigms.IEvent.Execute()");
                    // _debugger.Breakpoints.Add(Function: "Application.Application.InitTest()", 
                    //     Condition: "InstanceName == appStartEventConnector", 
                    //     ConditionType: dbgBreakpointConditionType.dbgBreakpointConditionTypeWhenTrue);

                    _debugger.Breakpoints.Add(File: "E:\\Code\\C#\\Projects\\GALADE-1.9.0\\GALADE_Standalone\\Application\\Application.cs", Line: 70);

                    var sb = new StringBuilder();
                    sb.AppendLine("Current debugger status:");
                    sb.Append(GetDebuggerInfo());
                    Logging.Log(sb.ToString());

                    await Task.Delay(5000);
                }

            }
            catch (Exception e)
            {
                Logging.Log(e);
            }

        }

        private string GetDebuggerInfo()
        {
            var sb = new StringBuilder();

            if (_debugger.CurrentStackFrame != null)
            {
                // var locals = _debugger.CurrentStackFrame.Locals.GetEnumerator();
                // while (locals.MoveNext())
                // {
                //
                //     var comObject = locals.Current;
                //     if (comObject == null) continue;
                //
                //     
                //
                // }

                // var methodName = _debugger.CurrentStackFrame.FunctionName;
                // sb.AppendLine($"Currently in method: {methodName}");

                var lastBreakpoint = _debugger.BreakpointLastHit;
                // var className = _debugger.BreakpointLastHit.;

                if (lastBreakpoint == null)
                {
                    sb.AppendLine("No breakpoints reached");
                    return sb.ToString();
                }

                // sb.AppendLine($"Currently in method {methodName} on line {lineNumber}");
                sb.AppendLine($"Status:");
                sb.AppendLine($"Method: {lastBreakpoint.FunctionName}");
                sb.AppendLine($"Line number: {lastBreakpoint.FileLine}");
                sb.AppendLine($"File path: {lastBreakpoint.File}");
                sb.AppendLine($"Condition: {lastBreakpoint.Condition}");
                sb.AppendLine($"Number of breakpoints: {_debugger.Breakpoints.Count}");

                // var functionName = "ProgrammingParadigms.EventConnector.ProgrammingParadigms.IEvent.Execute()";
                // _debugger.Breakpoints.Add(Function: functionName);
                // _debugger.Breakpoints.Add(Function: lastBreakpoint.FunctionName);
                // _debugger.Breakpoints.Add(File: lastBreakpoint.File, Line: lastBreakpoint.FileLine + 1);
                // _debugger.StepOver(); Thread.Sleep(1000);
                // _debugger.StepOver(); Thread.Sleep(1000);
                // _debugger.StepOver(); Thread.Sleep(1000);
            }
            else
            {
                sb.AppendLine("Stack frame is currently null");
            }

            return sb.ToString();
        }

        public VSDebugger()
        {

        }
    }
}
