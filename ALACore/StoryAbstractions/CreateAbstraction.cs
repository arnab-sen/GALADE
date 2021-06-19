using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using System.Windows;
using System.Windows.Media;
using System.IO;

namespace StoryAbstractions
{
    /// <summary>
    /// <para>Creates the contents of a class file for an ALA abstraction. Fields, properties, method stubs, and comment regions are all added based on the abstraction's ports.
    /// This includes adding placeholder implementation for implemented interfaces, although only IEvent, IDataFlow, and IUI are the only supported Programming Paradigms for now.</para>
    /// <para>This uses, but does not wire to, the ClassFileGenerator domain abstraction.</para>
    /// <para>Ports:</para>
    /// <para>1. IEvent start:</para>
    /// <para>2. IDataFlowB&lt;string&gt; classNameInput:</para>
    /// <para>3. IDataFlowB&lt;List&lt;string&gt;&gt; implementedPortsInput:</para>
    /// <para>4. IDataFlowB&lt;List&lt;string&gt;&gt; providedPortsInput:</para>
    /// <para>5. IDataFlow&lt;string&gt; fileContentsOutput:</para>
    /// </summary>
    public class CreateAbstraction : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Enums.ALALayer Layer { get; set; } = Enums.ALALayer.DomainAbstractions;
        public bool WriteFile { get; set; } = false;
        public string ClassName { get; set; }
        public List<string> ImplementedPorts { get; set; } = new List<string>();
        public List<string> AcceptedPorts { get; set; } = new List<string>();
        public string FilePath { get; set; } = "";
        public string NamespacePrefix { get; set; } = "";

        // Private fields

        // Input instances

        // Output instances

        // Ports
        private IDataFlowB<string> classNameInput;
        private IDataFlowB<List<string>> implementedPortsInput;
        private IDataFlowB<List<string>> providedPortsInput;
        private IDataFlow<string> fileContentsOutput;


        public CreateAbstraction()
        {

        }

        private void AddSupportedPortDetails(ClassFileGenerator cfg, string implementedPort, bool isImplemented = true)
        {
            var type = implementedPort.Split()[0];
            var name = implementedPort.Split()[1];

            if (type.StartsWith("IEvent") && !type.StartsWith("IEventB"))
            {
                if (isImplemented)
                {
                    List<string> methodBody = new List<string>();

                    if (Layer == Enums.ALALayer.StoryAbstractions)
                    {
                        cfg.AddField("EventConnector",
                                        $"{name}Connector",
                                        accessLevel: "private",
                                        defaultValue: $"new EventConnector() {{ InstanceName = \"{name}Connector\" }}",
                                        region: "Input instances");


                        methodBody.Add($"({name}Connector as IEvent).Execute();"); 
                    }

                    cfg.AddMethod("IEvent.Execute", region: $"{type} implementation", methodBody: methodBody);  
                }
                else
                {
                    if (Layer == Enums.ALALayer.StoryAbstractions)
                    {
                        cfg.AddField("EventConnector",
                                        $"{name}Connector",
                                        accessLevel: "private",
                                        defaultValue: $"new EventConnector() {{ InstanceName = \"{name}Connector\" }}",
                                        region: "Output instances"); 
                    }
                }
            }
            else if (type.StartsWith("IDataFlow"))
            {
                if (type.StartsWith("IDataFlowB"))
                {

                }
                else
                {
                    var internalDataType = type.Replace("IDataFlow<", "");
                    internalDataType = internalDataType.Substring(0, internalDataType.Length - 1);

                    if (isImplemented)
                    {
                        var setterBody = "";

                        if (Layer == Enums.ALALayer.StoryAbstractions)
                        {
                            cfg.AddField($"DataFlowConnector<{internalDataType}>", $"{name}Connector",
                                                accessLevel: "private",
                                                defaultValue: $"new DataFlowConnector<{internalDataType}>() {{ InstanceName = \"{name}Connector\" }}",
                                                region: "Input instances");

                            setterBody = $"{{({name}Connector as IDataFlow<{internalDataType}>).Data = value;}}"; 
                        }

                        cfg.AddProperty(internalDataType, $"{type}.Data", region: $"{type} implementation", setterBody: setterBody);
                    }
                    else
                    {
                        if (Layer == Enums.ALALayer.StoryAbstractions)
                        {
                            cfg.AddField($"Apply<{internalDataType}, {internalDataType}>", $"{name}Connector",
                                                accessLevel: "private",
                                                defaultValue: $"new Apply<{internalDataType}, {internalDataType}>() {{ InstanceName = \"{name}Connector\", Lambda = input => input }}",
                                                region: "Output instances"); 
                        }
                    }
                }
            }
            else if (type.StartsWith("IUI"))
            {
                cfg.AddMethod("IUI.GetWPFElement", returnType: "UIElement", region: $"{type} implementation");
            }

        }

        private string GetClassNameWithoutTypes(string name)
        {
            return name.Replace("<", ";").Split(new[] {';'})[0];
        }

        public string Create()
        {
            try
            {
                var baseListInlineComment = "";

                if (ImplementedPorts.Any())
                {
                    baseListInlineComment = $"// {ImplementedPorts.First().Split()[1]}";
                }

                baseListInlineComment = ImplementedPorts.Skip(1).Aggregate(baseListInlineComment, (current, port) => current + $", {port.Split()[1]}");

                var cfg = new ClassFileGenerator()
                {
                    Namespace = $"{NamespacePrefix}{Enum.GetName(typeof(Enums.ALALayer), Layer)}",
                    ClassName = ClassName,
                    ImplementedInterfaces = ImplementedPorts.Select(s => s.Split().First()).ToList(), // Just get interface types
                    BaseListInlineComment = baseListInlineComment, // Port names combined into a comment
                    IsInterface = Layer == Enums.ALALayer.ProgrammingParadigms
                };

                if (Layer != Enums.ALALayer.ProgrammingParadigms)
                {
                    cfg.Regions.Add("Public fields and properties");
                    cfg.Regions.Add("Private fields");
                    cfg.Regions.Add("Ports"); 
                    cfg.AddProperty("string", "InstanceName", accessLevel: "public", defaultValue: "\"Default\"", region: "Public fields and properties", isAutoProperty: true);
                }
                else
                {
                    cfg.Regions.Add("Properties");
                    cfg.Regions.Add("Methods");
                }

                // Add usings
                cfg.Usings.Add("System.Windows");

                if (Layer == Enums.ALALayer.ProgrammingParadigms)
                {
                    cfg.Usings.Add($"{NamespacePrefix}Libraries");
                }
                else if (Layer == Enums.ALALayer.DomainAbstractions)
                {
                    cfg.Usings.Add($"{NamespacePrefix}Libraries");
                    cfg.Usings.Add($"{NamespacePrefix}ProgrammingParadigms");
                }
                else if (Layer == Enums.ALALayer.StoryAbstractions)
                {
                    cfg.Usings.Add($"{NamespacePrefix}Libraries");
                    cfg.Usings.Add($"{NamespacePrefix}ProgrammingParadigms");
                    cfg.Usings.Add($"{NamespacePrefix}DomainAbstractions");
                }

                List<string> description = new List<string> { "" };
                int portCounter = 1;

                if (Layer != Enums.ALALayer.ProgrammingParadigms)
                {
                    if (Layer == Enums.ALALayer.StoryAbstractions)
                    {
                        cfg.Regions.Add("Input instances");
                        cfg.Regions.Add("Output instances");
                    };

                    description.Add("Ports:");

                    // Create implementation stubs
                    foreach (var implementedPort in ImplementedPorts)
                    {
                        var type = implementedPort.Split()[0];
                        cfg.Regions.Add($"{type} implementation");
                        AddSupportedPortDetails(cfg, implementedPort, isImplemented: true);

                        description.Add($"{portCounter}. {implementedPort.Replace("<", "&lt;").Replace(">", "&gt;")}:");
                        portCounter++;
                    }

                    foreach (var providedPort in AcceptedPorts)
                    {
                        var split = providedPort.Split();
                        if (split.Length < 2) continue;

                        var type = split[0];
                        var name = split[1];

                        if (type.StartsWith("List<"))
                        {
                            cfg.AddField(type, name, accessLevel: "private", region: "Ports", defaultValue: $"new {type}()"); 
                        }
                        else
                        {
                            cfg.AddField(type, name, accessLevel: "private", region: "Ports"); 
                        }

                        description.Add($"{portCounter}. {providedPort.Replace("<", "&lt;").Replace(">", "&gt;")}:");
                        portCounter++;

                        AddSupportedPortDetails(cfg, providedPort, isImplemented: false);
                    } 
                }

                cfg.Description = description;

                if (Layer == Enums.ALALayer.StoryAbstractions)
                {
                    // Add landmarks for wiring code insertion
                    List<string> constructorBody = new List<string>();
                    constructorBody.Add($"// BEGIN AUTO-GENERATED INSTANTIATIONS FOR {ClassName}");
                    constructorBody.Add($"// END AUTO-GENERATED INSTANTIATIONS FOR {ClassName}");
                    constructorBody.Add($"");

                    constructorBody.Add($"// BEGIN AUTO-GENERATED WIRING FOR {ClassName}");
                    constructorBody.Add($"// END AUTO-GENERATED WIRING FOR {ClassName}");
                    constructorBody.Add($"");

                    constructorBody.Add($"// BEGIN MANUAL INSTANTIATIONS FOR {ClassName}");
                    constructorBody.Add($"// END MANUAL INSTANTIATIONS FOR {ClassName}");
                    constructorBody.Add($"");

                    constructorBody.Add($"// BEGIN MANUAL WIRING FOR {ClassName}");
                    constructorBody.Add($"// END MANUAL WIRING FOR {ClassName}");

                    cfg.ConstructorBody = constructorBody;

                    // Add PostWiringInitialize
                    List<string> postWiringInitializeBody = new List<string>();
                    postWiringInitializeBody.Add("// Mapping to virtual ports");
                    // postWiringInitializeBody.Add("// Utilities.ConnectToVirtualPort(outputInstance, \"portOnOutputInstance\", portInStoryAbstraction);");
                    // postWiringInitializeBody.Add("");

                    foreach (var providedPort in AcceptedPorts)
                    {
                        var type = providedPort.Split()[0];
                        var name = providedPort.Split()[1];

                        // Connector should be an Apply<type, type>
                        if (type.StartsWith("IDataFlow") && !type.StartsWith("IDataFlowB"))
                        {
                            // postWiringInitializeBody.Add($"Utilities.ConnectToVirtualPort({name}Connector, \"output\", {name});"); 
                            postWiringInitializeBody.Add($"if ({name} != null) {name}Connector.WireTo({name}, \"output\");"); 
                        }
                        else if (type.StartsWith("IEvent") && !type.StartsWith("IEventB"))
                        {
                            // postWiringInitializeBody.Add($"Utilities.ConnectToVirtualPort({name}Connector, \"complete\", {name});");
                            postWiringInitializeBody.Add($"if ({name} != null) {name}Connector.WireTo({name}, \"complete\");");
                        }
                    }
                    postWiringInitializeBody.Add(""); 

                    postWiringInitializeBody.Add("// IDataFlowB and IEventB event handlers");
                    // postWiringInitializeBody.Add("// if (inputDataFlowBPort != null)");
                    // postWiringInitializeBody.Add("// {");
                    // postWiringInitializeBody.Add("//     inputDataFlowBPort.DataChanged += () => (inputInstance as IDataFlow<T>).Data = inputDataFlowBPort.Data;");
                    // postWiringInitializeBody.Add("// }");
                    foreach (var providedPort in AcceptedPorts)
                    {
                        var type = providedPort.Split()[0];
                        var name = providedPort.Split()[1];

                        // Connector should be an Apply<type, type>
                        if (type.StartsWith("IDataFlowB"))
                        {
                            postWiringInitializeBody.Add($"if ({name} != null)");
                            postWiringInitializeBody.Add($"{{");
                            postWiringInitializeBody.Add($"    {name}.DataChanged += () => ({name}Connector as IDataFlow<T>).Data = {name}.Data;");
                            postWiringInitializeBody.Add($"}}");
                            postWiringInitializeBody.Add("");
                        }
                        else if (type.StartsWith("IEventB"))
                        {
                            postWiringInitializeBody.Add($"if ({name} != null)");
                            postWiringInitializeBody.Add($"{{");
                            postWiringInitializeBody.Add($"    {name}.EventHappened += () => ({name}Connector as IEvent).Execute();");
                            postWiringInitializeBody.Add($"}}");
                            postWiringInitializeBody.Add("");
                        }
                    }

                    postWiringInitializeBody.Add("// Send out initial values");
                    postWiringInitializeBody.Add("// (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;");

                    cfg.Regions.Add("PostWiringInitialize");
                    cfg.AddMethod("PostWiringInitialize", accessLevel: "private", methodBody: postWiringInitializeBody, region: "PostWiringInitialize");
                }

                var classFileTemplateContents = cfg.BuildFile();

                if (WriteFile && Directory.Exists(Path.GetDirectoryName(FilePath)))
                {
                    var classNameWithoutTypes = GetClassNameWithoutTypes(ClassName);

                    File.WriteAllText(Path.Combine(FilePath, $"{classNameWithoutTypes}.cs"), classFileTemplateContents);

                    if (Layer == Enums.ALALayer.StoryAbstractions && File.Exists(Path.Combine(FilePath, $"baseStoryAbstraction.xmind")))
                    {
                        // Create xmind diagram 
                        File.Copy(Path.Combine(FilePath, $"baseStoryAbstraction.xmind"), Path.Combine(FilePath, $"{classNameWithoutTypes}.xmind"));
                    }

                }

                if (fileContentsOutput != null) fileContentsOutput.Data = classFileTemplateContents;
                return classFileTemplateContents;
            }
            catch (Exception e)
            {

            }

            return "";
        }

        private void PostWiringInitialize()
        {
            // Mapping to virtual ports
            
            // DataChanged lambdas
            if (classNameInput != null) classNameInput.DataChanged += () => ClassName = classNameInput.Data;
            if (implementedPortsInput != null) implementedPortsInput.DataChanged += () => ImplementedPorts = implementedPortsInput.Data;
            if (providedPortsInput != null) providedPortsInput.DataChanged += () => AcceptedPorts = providedPortsInput.Data;

        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Create();
        }

    }
}
