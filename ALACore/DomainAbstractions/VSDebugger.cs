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

namespace DomainAbstractions
{
    /// <summary>
    /// A class that hooks onto and interacts with the debugger of the first opened instance of Visual Studio 2019.
    /// </summary>
    public class VSDebugger : IEvent // start
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private Debugger _debugger;
        private DebuggerEvents _debuggerEvents; // Reference to this must be kept alive in order to use the events, so we define it here

        // Ports

        // IEvent implementation
        void IEvent.Execute()
        {
            Test();
        }

        // Methods
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
