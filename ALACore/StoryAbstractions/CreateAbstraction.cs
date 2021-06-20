using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <para>5. IDataFlow&lt;string&gt; fileContentsOutput: The folder that should contain folders for each layer. The created abstraction file will be automatically added to this folder.</para>
    /// <para>6. IDataFlowB&lt;string&gt; baseFolderPathInput:</para>
    /// </summary>
    public class CreateAbstraction : IEvent, IDataFlow<AbstractionModel> // create, createFromModel
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public Enums.ALALayer Layer { get; set; } = Enums.ALALayer.DomainAbstractions;
        public string ClassName { get; set; }
        public List<Port> ImplementedPorts { get; set; } = new List<Port>();
        public List<Port> AcceptedPorts { get; set; } = new List<Port>();
        public string NamespacePrefix { get; set; } = "";

        // Private fields

        // Input instances

        // Output instances

        // Ports
        private IDataFlowB<string> classNameInput;
        private IDataFlowB<List<string>> implementedPortsInput;
        private IDataFlowB<List<string>> providedPortsInput;
        private IDataFlow<string> fileContentsOutput;
        private IDataFlow<string> filePathOutput;
        private IDataFlowB<string> baseFolderPathInput;


        public CreateAbstraction()
        {

        }

        private void AddSupportedPortDetails(ClassFileGenerator cfg, Port port, bool isImplemented = true)
        {
            var type = port.Type;
            var name = port.Name;

            if (type == "IEvent")
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

                    cfg.AddMethod("IEvent.Execute", region: "IEvent implementation", methodBody: methodBody);  
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
                            cfg.AddField($"DataFlowConnector<{internalDataType}>", $"{name}Connector",
                                                accessLevel: "private",
                                                defaultValue: $"new DataFlowConnector<{internalDataType}>() {{ InstanceName = \"{name}Connector\" }}",
                                                region: "Output instances"); 
                        }
                    }
                }
            }
            else if (type.StartsWith("IUI"))
            {
                cfg.AddMethod("IUI.GetWPFElement", returnType: "UIElement", region: "IUI implementation");
            }
            else if (type == "IEventHandler")
            {
                cfg.AddProperty("object", $"Sender", region: "IEventHandler implementation");

                cfg.AddMethod("IEventHandler.Subscribe", region: "IEventHandler implementation");
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
                    baseListInlineComment = $"// {ImplementedPorts.First().Name}";
                }

                baseListInlineComment = ImplementedPorts.Skip(1).Aggregate(baseListInlineComment, (current, port) => current + $", {port.Name}");

                var cfg = new ClassFileGenerator()
                {
                    Namespace = $"{NamespacePrefix}{Enum.GetName(typeof(Enums.ALALayer), Layer)}",
                    ClassName = ClassName,
                    ImplementedInterfaces = ImplementedPorts.Select(s => s.Type).ToList(), // Just get interface types
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
                        var type = implementedPort.Type;
                        cfg.Regions.Add($"{type} implementation");
                        AddSupportedPortDetails(cfg, implementedPort, isImplemented: true);

                        description.Add($"{portCounter}. {implementedPort.FullName.Replace(">", "&gt;")}:");
                        portCounter++;
                    }

                    foreach (var providedPort in AcceptedPorts)
                    {
                        var type = providedPort.Type;
                        var name = providedPort.Name;

                        if (type.StartsWith("List<"))
                        {
                            cfg.AddField(type, name, accessLevel: "private", region: "Ports", defaultValue: $"new {type}()"); 
                        }
                        else
                        {
                            cfg.AddField(type, name, accessLevel: "private", region: "Ports"); 
                        }

                        description.Add($"{portCounter}. {providedPort.FullName.Replace("<", "&lt;").Replace(">", "&gt;")}:");
                        portCounter++;

                        AddSupportedPortDetails(cfg, providedPort, isImplemented: false);
                    } 
                }

                cfg.Description = description;

                // Add PostWiringInitialize
                List<string> postWiringInitializeBody = new List<string>();

                bool postWiringHeaderAdded = false;
                // postWiringInitializeBody.Add("// if (inputDataFlowBPort != null)");
                // postWiringInitializeBody.Add("// {");
                // postWiringInitializeBody.Add("//     inputDataFlowBPort.DataChanged += () => (inputInstance as IDataFlow<T>).Data = inputDataFlowBPort.Data;");
                // postWiringInitializeBody.Add("// }");
                foreach (var accepted in AcceptedPorts.Where(p => p.IsReversePort))
                {
                    if (!postWiringHeaderAdded)
                    {
                        postWiringInitializeBody.Add("// IDataFlowB and IEventB event handlers");
                        postWiringHeaderAdded = true;
                    }

                    var type = accepted.Type;
                    var name = accepted.Name;

                    if (type.StartsWith("IDataFlowB"))
                    {
                        postWiringInitializeBody.Add($"if ({name} != null)");
                        postWiringInitializeBody.Add($"{{");
                        postWiringInitializeBody.Add($"    {name}.DataChanged += () => {{ }}");
                        postWiringInitializeBody.Add($"}}");
                        postWiringInitializeBody.Add("");
                    }
                    else if (type.StartsWith("IEventB"))
                    {
                        postWiringInitializeBody.Add($"if ({name} != null)");
                        postWiringInitializeBody.Add($"{{");
                        postWiringInitializeBody.Add($"    {name}.EventHappened += () => {{ }}");
                        postWiringInitializeBody.Add($"}}");
                        postWiringInitializeBody.Add("");
                    }
                }


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

                    postWiringInitializeBody.Add("// Mapping to virtual ports");
                    // postWiringInitializeBody.Add("// Utilities.ConnectToVirtualPort(outputInstance, \"portOnOutputInstance\", portInStoryAbstraction);");
                    // postWiringInitializeBody.Add("");

                    foreach (var acceptedPort in AcceptedPorts)
                    {
                        var type = acceptedPort.Type;
                        var name = acceptedPort.Name;

                        if (type.StartsWith("IDataFlow") && !type.StartsWith("IDataFlowB"))
                        {
                            // postWiringInitializeBody.Add($"Utilities.ConnectToVirtualPort({name}Connector, \"output\", {name});"); 
                            postWiringInitializeBody.Add($"if ({name} != null) {name}Connector.WireTo({name}, \"fanoutList\");"); 
                        }
                        else if (type.StartsWith("IEvent") && !type.StartsWith("IEventB"))
                        {
                            // postWiringInitializeBody.Add($"Utilities.ConnectToVirtualPort({name}Connector, \"complete\", {name});");
                            postWiringInitializeBody.Add($"if ({name} != null) {name}Connector.WireTo({name}, \"complete\");");
                        }
                    }
                    postWiringInitializeBody.Add(""); 

                    postWiringInitializeBody.Add("// Send out initial values");
                    postWiringInitializeBody.Add("// (instanceNeedingInitialValue as IDataFlow<T>).Data = defaultValue;");
                }

                cfg.Regions.Add("PostWiringInitialize");
                cfg.AddMethod("PostWiringInitialize", accessLevel: "private", methodBody: postWiringInitializeBody, region: "PostWiringInitialize");

                var classFileTemplateContents = cfg.BuildFile();

                if (baseFolderPathInput != null)
                {
                    var basePath = baseFolderPathInput.Data;

                    var classNameWithoutTypes = GetClassNameWithoutTypes(ClassName);
                    var filePath = Path.Combine(basePath, $"{Layer}/{classNameWithoutTypes}.cs");

                    File.WriteAllText(filePath, classFileTemplateContents);

                    if (filePathOutput != null) filePathOutput.Data = filePath;

                    Process.Start(filePath);
                }

                if (fileContentsOutput != null) fileContentsOutput.Data = classFileTemplateContents;
                return classFileTemplateContents;
            }
            catch (Exception e)
            {

            }

            return "";
        }

        // IEvent implementation
        void IEvent.Execute()
        {
            Create();
        }

        // IDataFlow<AbstractionModel> implementation
        AbstractionModel IDataFlow<AbstractionModel>.Data
        {
            get => default;
            set => CreateFromAbstractionModel(value);
        }

        private void CreateFromAbstractionModel(AbstractionModel model)
        {
            ClassName = model.FullType;
            ImplementedPorts = model.GetImplementedPorts().ToList();
            AcceptedPorts = model.GetAcceptedPorts().ToList();
            Layer = model.Layer;

            Create();
        }

    }
}
