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
        private DebuggerEvents _debuggerEvents; // Reference to this must be kept alive in order to use the events, so we define it here
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

            try
            {
                DTE2 dte = (DTE2)Marshal.GetActiveObject($"VisualStudio.DTE.{_mappingVSToDTEVersion[VSVersion]}.0");
                _debugger = dte.Debugger;
                _debuggerEvents = dte.Events.DebuggerEvents;
            }
            catch (Exception e)
            {
                Logging.Log($"VSDebugger: No instance of Visual Studio {VSVersion} found... failed to connect.\nException: {e}");
            }
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

        /// <summary>
        /// Deletes all breakpoints in the file at the given filepath.
        /// </summary>
        /// <param name="filePath"></param>
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

        public VSDebugger()
        {

        }
    }
}
