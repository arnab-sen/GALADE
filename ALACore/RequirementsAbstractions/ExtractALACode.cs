using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RequirementsAbstractions
{
    /// <summary>
    /// <para>Extracts code between code generation landmarks.</para>
    /// <para>Ports:</para>
    /// <para>1. IDataFlow&lt;string&gt; codeInput:</para>
    /// <para>2. IDataFlow&lt;Tuple&lt;string, List&lt;string&gt;&gt;&gt; selectedDiagram: A (diagramName, instantiationsAndWireTos) tuple.</para>
    /// <para>3. IDataFlow&lt;Dictionary&lt;string, List&lt;string&gt;&gt;&gt; allDiagrams: A dictionary of (diagramName, instantiationsAndWireTos) pairs.</para>
    /// </summary>
    public class ExtractALACode : IDataFlow<string> // codeInput
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Instantiations => instantiationCodeOutputConnector.Data;
        public string Wiring => wiringCodeOutputConnector.Data;
        public string SourceCode { get; set; } = "";
        public Dictionary<string, List<string>> NodeToDiagramMapping { get; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// The landmarks are stored as follows:
        /// <code>Landmarks[0] = "// BEGIN AUTO-GENERATED INSTANTIATIONS FOR (NAME)"</code>
        /// <code>Landmarks[1] = "// END AUTO-GENERATED INSTANTIATIONS FOR (NAME)"</code>
        /// <code>Landmarks[2] = "// BEGIN AUTO-GENERATED WIRING FOR (NAME)"</code>
        /// <code>Landmarks[3] = "// END AUTO-GENERATED WIRING FOR (NAME)"</code>
        /// </summary>
        public string[] Landmarks { get; } = new[]
        {
            "// BEGIN AUTO-GENERATED INSTANTIATIONS",
            "// END AUTO-GENERATED INSTANTIATIONS",
            "// BEGIN AUTO-GENERATED WIRING",
            "// END AUTO-GENERATED WIRING"
        };

        public string CurrentDiagramName { get; set; }

        // Private fields

        // Ports
        private IDataFlow<string> instantiationCodeOutput;
        private IDataFlow<string> wiringCodeOutput;
        private IDataFlow<Tuple<string, List<string>>> selectedDiagram;
        private IDataFlow<Dictionary<string, Tuple<string, List<string>>>> allDiagrams;
        private IEvent diagramSelected;

        // Input instances
        private DataFlowConnector<string> codeInputConnector = new DataFlowConnector<string>() { InstanceName = "codeInputConnector" };

        // Output instances
        private DataFlowConnector<string> instantiationCodeOutputConnector = new DataFlowConnector<string>() { InstanceName = "instantiationCodeOutputConnector" };
        private DataFlowConnector<string> wiringCodeOutputConnector = new DataFlowConnector<string>() { InstanceName = "wiringCodeOutputConnector" };


        // IDataFlow<string> implementation
        string IDataFlow<string>.Data
        {
            get => default;
            set
            {
                SourceCode = value;
                ExtractCode(SourceCode);
            }
        }

        // Methods
        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            if (instantiationCodeOutput != null) instantiationCodeOutputConnector.WireTo(instantiationCodeOutput);
            if (wiringCodeOutput != null) wiringCodeOutputConnector.WireTo(wiringCodeOutput);

            // IDataFlowB and IEventB event handlers
            // Send out initial values
            // (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;
        }

        public void ExtractCode(string code, string chosenDiagramName = "")
        {
            var codeLines = code.Split(new []{ Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var unnamedInstantiationsCounter = 0;
            var unnamedWireTosCounter = 0;
            var instantiations = new Dictionary<string, List<string>>();
            var wireTos = new Dictionary<string, List<string>>();

            var lineIndex = 0;
            while (lineIndex < codeLines.Length)
            {
                var candidate = codeLines[lineIndex].Trim();
                string diagramName = "";

                if (candidate.StartsWith("// BEGIN AUTO-GENERATED INSTANTIATIONS"))
                {
                    if (candidate.StartsWith("// BEGIN AUTO-GENERATED INSTANTIATIONS FOR"))
                    {
                        var extracted = InverseStringFormat.GetInverseStringFormat(candidate, @"// BEGIN AUTO-GENERATED INSTANTIATIONS FOR {diagramName}");

                        if (extracted.TryGetValue("diagramName", out diagramName))
                        {
                            instantiations[diagramName] = new List<string>();
                            wireTos[diagramName] = new List<string>();
                        }
                        else
                        {
                            instantiations[$"unnamed{unnamedInstantiationsCounter}"] = new List<string>();
                            wireTos[$"unnamed{unnamedInstantiationsCounter}"] = new List<string>();
                            unnamedInstantiationsCounter++;
                        }
                    }
                    else
                    {
                        diagramName = $"unnamed{unnamedInstantiationsCounter}";
                        instantiations[diagramName] = new List<string>();
                        wireTos[diagramName] = new List<string>();
                        unnamedInstantiationsCounter++;
                    }

                    // Get code between this landmark and the next "// END AUTO-GENERATED" landmark
                    lineIndex++;
                    while (lineIndex < codeLines.Length)
                    {
                        var innerCandidate = codeLines[lineIndex].Trim();
                        if (innerCandidate.StartsWith("// END AUTO-GENERATED"))
                        {
                            break;
                        }
                        else
                        {
                            if (instantiations.ContainsKey(diagramName))
                            {
                                instantiations[diagramName].Add(innerCandidate);
                            }

                        }

                        lineIndex++;
                    }
                }
                else if (candidate.StartsWith("// BEGIN AUTO-GENERATED WIRING"))
                {
                    if (candidate.StartsWith("// BEGIN AUTO-GENERATED WIRING FOR"))
                    {
                        var extracted = InverseStringFormat.GetInverseStringFormat(candidate, @"// BEGIN AUTO-GENERATED WIRING FOR {diagramName}");

                        extracted.TryGetValue("diagramName", out diagramName);
                        if (string.IsNullOrEmpty(diagramName))
                        {
                            diagramName = $"unnamed{unnamedWireTosCounter}";
                            unnamedWireTosCounter++;
                        }
                    }
                    else
                    {
                        diagramName = $"unnamed{unnamedWireTosCounter}";
                        unnamedWireTosCounter++;
                    }

                    // Get code between this landmark and the next "// END AUTO-GENERATED" landmark
                    lineIndex++;
                    while (lineIndex < codeLines.Length)
                    {
                        var innerCandidate = codeLines[lineIndex].Trim();
                        if (innerCandidate.StartsWith("// END AUTO-GENERATED"))
                        {
                            break;
                        }
                        else
                        {
                            if (wireTos.ContainsKey(diagramName))
                            {
                                wireTos[diagramName].Add(innerCandidate);
                            }

                        }

                        lineIndex++;
                    }
                }

                lineIndex++;
            }

            var allDiagramsDictionary = new Dictionary<string, Tuple<string, List<string>>>();
            foreach (var kvp in instantiations)
            {
                allDiagramsDictionary[kvp.Key] = Tuple.Create(kvp.Key, new List<string>(kvp.Value));
            }

            foreach (var kvp in wireTos)
            {
                if (!allDiagramsDictionary.ContainsKey(kvp.Key))
                {
                    allDiagramsDictionary[kvp.Key] = Tuple.Create(kvp.Key, new List<string>(kvp.Value));
                }
                else
                {
                    allDiagramsDictionary[kvp.Key].Item2.AddRange(kvp.Value);
                }
            }

            CreateNodeToDiagramMappings(allDiagramsDictionary, NodeToDiagramMapping);

            if (allDiagrams != null) allDiagrams.Data = allDiagramsDictionary;

            if (string.IsNullOrWhiteSpace(chosenDiagramName))
            {
                // Ask the user to choose a diagram if there are multiple, otherwise just select the first diagram
                if (wireTos.Keys.Count > 1)
                {
                    GetUserSelection(instantiations.Keys.ToList(), instantiations, wireTos);
                }
                else
                {
                    Output(wireTos.Keys.First(), instantiations, wireTos);
                }   
            }
            else
            {
                Output(chosenDiagramName, instantiations, wireTos);
            }

        }

        private void GetUserSelection(List<string> diagramNames, Dictionary<string, List<string>> instantiations, Dictionary<string, List<string>> wireTos)
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            var startProcess = new EventConnector() {InstanceName="startProcess"};
            var userInputWindow = new PopupWindow(title:"Select diagram") {Width=300,InstanceName="userInputWindow"};
            var id_34cec60b4bb5466fb013c31972c226a8 = new Vertical() {InstanceName="id_34cec60b4bb5466fb013c31972c226a8"};
            var id_951fa16a0fec497fb615934f6ad17db8 = new UIConfig() {InstanceName="id_951fa16a0fec497fb615934f6ad17db8",HorizAlignment="middle"};
            var id_33069e19e1c5462daa00a71797520fd3 = new Text(text:"Please select a diagram to open:") {InstanceName="id_33069e19e1c5462daa00a71797520fd3"};
            var id_3e5649374fac4568bfaec0d6fe12aa71 = new Horizontal() {Ratios=new[]{3, 1},InstanceName="id_3e5649374fac4568bfaec0d6fe12aa71"};
            var id_8e32e9029d924f53a7cf22e6d96056cc = new DropDownMenu() {InstanceName="id_8e32e9029d924f53a7cf22e6d96056cc",Items=diagramNames};
            var id_a3b86160ecfb40959d88ccc083aa5ede = new UIConfig() {InstanceName="id_a3b86160ecfb40959d88ccc083aa5ede",UniformMargin=5};
            var id_e8e4b3fe1afb4c04a2946359be97f1c5 = new UIConfig() {InstanceName="id_e8e4b3fe1afb4c04a2946359be97f1c5",UniformMargin=5};
            var id_2fe458d57b404a49bac834c60d8f3aef = new Button(title:"OK") {InstanceName="id_2fe458d57b404a49bac834c60d8f3aef"};
            var selectedDiagramConnector = new DataFlowConnector<string>() {InstanceName="selectedDiagramConnector"};
            var id_95e6c08c5e1b47b3a4f2088e95a0089f = new EventConnector() {InstanceName="id_95e6c08c5e1b47b3a4f2088e95a0089f"};
            var id_403a812ba97742c69d5f39179bf5a4ed = new EventLambda() {InstanceName="id_403a812ba97742c69d5f39179bf5a4ed",Lambda=() =>{    CurrentDiagramName = selectedDiagramConnector.Data;    Output(CurrentDiagramName, instantiations, wireTos);}};
            // END AUTO-GENERATED INSTANTIATIONS
            
            // BEGIN AUTO-GENERATED WIRING
            startProcess.WireTo(userInputWindow, "fanoutList");
            userInputWindow.WireTo(id_34cec60b4bb5466fb013c31972c226a8, "children");
            id_34cec60b4bb5466fb013c31972c226a8.WireTo(id_951fa16a0fec497fb615934f6ad17db8, "children");
            id_951fa16a0fec497fb615934f6ad17db8.WireTo(id_33069e19e1c5462daa00a71797520fd3, "child");
            id_34cec60b4bb5466fb013c31972c226a8.WireTo(id_3e5649374fac4568bfaec0d6fe12aa71, "children");
            id_3e5649374fac4568bfaec0d6fe12aa71.WireTo(id_a3b86160ecfb40959d88ccc083aa5ede, "children");
            id_a3b86160ecfb40959d88ccc083aa5ede.WireTo(id_8e32e9029d924f53a7cf22e6d96056cc, "child");
            id_3e5649374fac4568bfaec0d6fe12aa71.WireTo(id_e8e4b3fe1afb4c04a2946359be97f1c5, "children");
            id_e8e4b3fe1afb4c04a2946359be97f1c5.WireTo(id_2fe458d57b404a49bac834c60d8f3aef, "child");
            id_8e32e9029d924f53a7cf22e6d96056cc.WireTo(selectedDiagramConnector, "selectedItem");
            id_2fe458d57b404a49bac834c60d8f3aef.WireTo(id_95e6c08c5e1b47b3a4f2088e95a0089f, "eventButtonClicked");
            id_95e6c08c5e1b47b3a4f2088e95a0089f.WireTo(userInputWindow, "fanoutList");
            id_95e6c08c5e1b47b3a4f2088e95a0089f.WireTo(id_403a812ba97742c69d5f39179bf5a4ed, "fanoutList");
            // END AUTO-GENERATED WIRING

            userInputWindow.InitialiseContent();
            (startProcess as IEvent).Execute();

        }

        private void Output(string diagramName, Dictionary<string, List<string>> instantiations, Dictionary<string, List<string>> wireTos)
        {
            CurrentDiagramName = diagramName;

            Landmarks[0] = $"// BEGIN AUTO-GENERATED INSTANTIATIONS FOR {CurrentDiagramName}";
            Landmarks[1] = $"// END AUTO-GENERATED INSTANTIATIONS FOR {CurrentDiagramName}";
            Landmarks[2] = $"// BEGIN AUTO-GENERATED WIRING FOR {CurrentDiagramName}";
            Landmarks[3] = $"// END AUTO-GENERATED WIRING FOR {CurrentDiagramName}";

            if (selectedDiagram != null)
            {
                var combined = new List<string>();
                if (instantiations.ContainsKey(CurrentDiagramName)) combined.AddRange(instantiations[CurrentDiagramName]);
                if (wireTos.ContainsKey(CurrentDiagramName)) combined.AddRange(wireTos[CurrentDiagramName]);
                selectedDiagram.Data = Tuple.Create(CurrentDiagramName, combined);
            }

            if (instantiationCodeOutputConnector != null) instantiationCodeOutputConnector.Data = ConnectLines(instantiations[CurrentDiagramName]);
            if (wiringCodeOutputConnector != null) wiringCodeOutputConnector.Data = ConnectLines(wireTos[CurrentDiagramName]);    
            diagramSelected?.Execute();
        }

        private string ConnectLines(List<string> lines)
        {
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a mapping for each node to every diagram that it is found in.
        /// </summary>
        public static void CreateNodeToDiagramMappings(Dictionary<string, Tuple<string, List<string>>> allDiagrams, Dictionary<string, List<string>> mapping)
        {
            var parser = new CodeParser();
            mapping.Clear();

            foreach (var diagram in allDiagrams)
            {
                var diagramName = diagram.Key;
                var instantiationsAndWireTos = diagram.Value.Item2;

                foreach (var instantiationOrWireTo in instantiationsAndWireTos)
                {
                    if (string.IsNullOrWhiteSpace(instantiationOrWireTo)) continue;
                    var line = instantiationOrWireTo.Trim();

                    // If the line is a WireTo call
                    if (Regex.IsMatch(line, @"[\w_\d]+.WireTo\("))
                    {
                        try
                        {
                            var inv = InverseStringFormat.GetInverseStringFormat(line, @"{A}.WireTo({B},{sourcePort});");
                            var instanceNameA = inv["A"];
                            if (!mapping.ContainsKey(instanceNameA)) mapping[instanceNameA] = new List<string>();
                            if (!mapping[instanceNameA].Contains(diagramName)) mapping[instanceNameA].Add(diagramName);

                            var instanceNameB = inv["B"];
                            if (!mapping.ContainsKey(instanceNameB)) mapping[instanceNameB] = new List<string>();
                            if (!mapping[instanceNameB].Contains(diagramName)) mapping[instanceNameB].Add(diagramName);
                        }
                        catch (Exception e)
                        {
                            Logging.Log($"Failed to parse WireTo info for \"{line}\" in ExtractALACode:\n{e}");
                        }
                    }
                    else
                    {
                        try
                        {
                            var node = parser.GetRoot(line).DescendantNodes().OfType<VariableDeclarationSyntax>().First();
                            var instanceName = node.Variables.First().Identifier.Text;
                            if (!mapping.ContainsKey(instanceName)) mapping[instanceName] = new List<string>();

                            // The diagram that contains the instance as a non-reference node should be at the top of the list
                            if (!mapping[instanceName].Contains(diagramName)) mapping[instanceName].Insert(0, diagramName); 
                        }
                        catch (Exception e)
                        {
                            Logging.Log($"Failed to parse instantiation info for \"{line}\" in ExtractALACode:\n{e}");
                        }
                    }
                }
            }

            var multiNodes = mapping.Where(kvp => kvp.Value.Count > 1).ToList();
        }
        
        /// <summary>
        /// <para></para>
        /// </summary>
        public ExtractALACode()
        {

        }
    }
}
