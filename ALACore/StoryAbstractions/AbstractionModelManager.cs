using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Microsoft.CodeAnalysis;
using Microsoft.Win32;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace StoryAbstractions
{
    public class AbstractionModelManager
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public List<string> ProgrammingParadigms { get; set; }

        // Private fields
        private FileBrowser _fileBrowser;
        private FolderBrowser _folderBrowser;
        private FileReader _fileReader;
        private Dictionary<string, AbstractionModel> _abstractionModels = new Dictionary<string, AbstractionModel>();

        // Ports

        // Methods
        public void ClearAbstractions()
        {
            _abstractionModels.Clear();
        }

        public void OpenFile(string filePath = "")
        {
            if (string.IsNullOrEmpty(filePath))
            {
                (_fileBrowser as IEvent).Execute();
            }
            else
            {
                (_fileReader as IDataFlow<string>).Data = filePath;
            }
        }

        public AbstractionModel CreateDummyAbstractionModel(string type)
        {
            var model = new AbstractionModel()
            {
                Type = type,
                Name = ""
            };

            model.AddImplementedPort("Port", "input");
            model.AddAcceptedPort("Port", "output");

            return model;
        }

        public AbstractionModel CreateAbstractionModel(string code, string path = "")
        {
            var model = new AbstractionModel();
            model.SourceCode = code;
            model.CodeFilePath = path;

            var parser = new CodeParser();

            var classNode = parser.GetClasses(code).First() as ClassDeclarationSyntax;
            
            SetType(classNode, model);
            SetImplementedPorts(classNode, model);
            SetAcceptedPorts(classNode, model);
            SetProperties(classNode, model);
            SetFields(classNode, model);
            SetConstructorArgs(classNode, model);
            SetDocumentation(classNode, model);

            _abstractionModels[model.Type] = model;

            return model;
        }

        public AbstractionModel CreateAbstractionModelFromPath(string path)
        {
            if (File.Exists(path))
            {
                var code = File.ReadAllText(path);

                var model = CreateAbstractionModel(code, path: path);
                return model;
            }

            return null;
        }

        public AbstractionModel GetAbstractionModel(string type) => _abstractionModels.ContainsKey(type) ? _abstractionModels[type] : null;
        public List<string> GetAbstractionTypes() => _abstractionModels.Keys.ToList();

        private bool MatchStartOfString(string candidate, IEnumerable<string> set, string prefix = "", string suffix = "")
        {
            bool matches = false;

            foreach (var str in set)
            {
                if (candidate.StartsWith(prefix + str + suffix))
                {
                    matches = true;
                    break;
                }
            }

            return matches;
        }

        public void SetType(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            var identifier = classNode.Identifier.ToString();

            var type = identifier;
            model.Type = type;
            model.FullType = model.Type;

            // Generics
            if (classNode.TypeParameterList != null)
            {
                model.FullType += classNode.TypeParameterList.ToString();

                var generics = classNode.TypeParameterList.Parameters;
                model.SetGenerics(generics.Select(s => s.ToString()));
            }
        }

        public void SetImplementedPorts(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            SetPorts(classNode, model, isInputPort: true);
        }

        public void SetAcceptedPorts(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            SetPorts(classNode, model, isInputPort: false);
        }

        private string ParsePortDocumentation(string rawDoc)
        {
            var sb = new StringBuilder();
            var rawLines = rawDoc.Split(new [] { Environment.NewLine }, StringSplitOptions.None);

            var latestNewLineIndex = -1;
            for (int i = 0; i < rawLines.Length; i++)
            {
                if (string.IsNullOrEmpty(rawLines[i])) latestNewLineIndex = i;
            }

            for (int i = latestNewLineIndex + 1; i < rawLines.Length; i++)
            {
                if (string.IsNullOrEmpty(rawLines[i])) continue;

                var firstClean = rawLines[i];
                firstClean = firstClean.Trim().Trim(new[] {'/', '*'});

                sb.AppendLine(firstClean);
            }

            var parsedDoc = ParseDocumentation(sb.ToString());

            return parsedDoc;
        }

        private string TypeWithoutGenerics(string type)
        {
            if (type.Contains("<"))
            {
                var split = type.Split(new[] {'<'});
                return split.FirstOrDefault();
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Reverse ports are output ports that are implemented ports, or input ports that are accepted ports. The main use case of reverse ports is to allow for fan-in lists,
        /// as C# does not allow a class to implement multiple interfaces of the same type. A fan-in list would look like a fan-out list but with the interface type being a reverse port type.
        /// <para>For example, a typical fan-out port looks like:</para>
        /// <code>List&lt;IDataFlow&lt;string&gt;&gt; outputs</code>
        /// <para>And a fan-in list would look like:</para>
        /// <code>List&lt;IDataFlow_B&lt;string&gt;&gt; inputs</code>
        /// <para>Reverse ports types are expected to end with "_B", although for legacy support, "IDataFlowB" and "IEventB" are also supported.</para>
        /// </summary>
        /// <param name="portType"></param>
        /// <returns></returns>
        private bool IsReversePort(string portType)
        {
            if (portType.StartsWith("List<"))
            {
                portType = Regex.Match(portType, @"(?<=List<).+(?=>)").Value;
            }

            if (portType.Contains("<")) portType = portType.Split('<').First();

            var isReverse = portType == "IDataFlowB" || portType == "IEventB" || portType.EndsWith("_B");

            return isReverse;
        }

        private void SetPorts(ClassDeclarationSyntax classNode, AbstractionModel model, bool isInputPort = false)
        {
            try
            {
                if (isInputPort)
                {
                    var parser = new CodeParser();

                    var baseList = (parser.GetBaseObjects(classNode));
                    var portNodeList = (baseList.First() as BaseListSyntax)?.Types.ToList();

                    if (portNodeList == null) return;
                    var portSyntaxNodes = portNodeList.Where(n => MatchStartOfString(n.ToString(), ProgrammingParadigms));

                    var docLines = model.SourceCode.Split(new [] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var classDeclaration = docLines.First(line => line.Trim().Contains($"class {model.Type}"));

                    var implementedPortNames = new List<string>();
                    if (classDeclaration.Contains("//"))
                    {
                        // Get implemented port names
                        // string implementedPortsInlineComment = baseList.LastOrDefault()?.GetTrailingTrivia().ToString().Trim(new []{' ', '/', '\r', '\n'}) ?? "";
                        var implementedPortsInlineComment =
                            classDeclaration.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

                        if (!string.IsNullOrEmpty(implementedPortsInlineComment))
                        {
                            implementedPortNames = implementedPortsInlineComment.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                        } 
                    }

                    var modelGenerics = model.GetGenerics();

                    var implementedPortCount = 0;
                    foreach (var portSyntaxNode in portSyntaxNodes)
                    {
                        var port = new Port()
                        {
                            Type = portSyntaxNode.Type.ToString()
                        };

                        if (implementedPortCount < implementedPortNames.Count)
                        {
                            port.Name = implementedPortNames[implementedPortCount];
                        }
                        else
                        {
                            port.Name = "?" + port.Type;
                        }

                        // Handle reverse ports (e.g. IDataFlowB and IEventB)
                        // var typeWithoutGenerics = TypeWithoutGenerics(port.Type);
                        // port.IsReversePort = typeWithoutGenerics.EndsWith("B");
                        port.IsReversePort = IsReversePort(port.Type);

                        if (port.IsReversePort)
                        {
                            port.IsInputPort = false;
                            model.AddAcceptedPort(port.Type, port.Name, true);
                        }
                        else
                        {
                            port.IsInputPort = true;
                            model.AddImplementedPort(port.Type, port.Name); 
                        }

                        var indexList = new List<int>();

                        if (portSyntaxNode.Type is GenericNameSyntax gen)
                        {
                            var portGenerics = gen
                                .DescendantNodesAndSelf().OfType<GenericNameSyntax>()
                                .SelectMany(n => n.TypeArgumentList.Arguments.Select(a => a.ToString()))
                                .ToHashSet();

                            foreach (var portGeneric in portGenerics)
                            {
                                var index = modelGenerics.IndexOf(portGeneric);
                                if (index != -1) indexList.Add(index);
                            }
                        }

                        model.AddPortGenericIndices(port.Name, indexList);
                        implementedPortCount++;
                    }
                }
                else
                {
                    var parser = new CodeParser()
                    {
                        AccessLevel = "private"
                    };

                    var privateFields = parser.GetFields(classNode);
                    var portSyntaxNodes = privateFields.Where(n =>
                            n is FieldDeclarationSyntax field && 
                            (MatchStartOfString(field.Declaration.Type.ToString(), ProgrammingParadigms) ||
                             MatchStartOfString(field.Declaration.Type.ToString(), ProgrammingParadigms, prefix:"List<")))
                        .Select(s => s as FieldDeclarationSyntax);


                    var modelGenerics = model.GetGenerics();
                    foreach (var portSyntaxNode in portSyntaxNodes)
                    {
                        var port = new Port()
                        {
                            Type = portSyntaxNode.Declaration.Type.ToString(),
                            Name = portSyntaxNode.Declaration.Variables.First().Identifier.ToString(),
                        };

                        port.Description = portSyntaxNode.HasLeadingTrivia ? ParsePortDocumentation(portSyntaxNode.GetLeadingTrivia().ToString()) : "";

                        // Handle reverse ports (e.g. IDataFlowB and IEventB)
                        // var typeWithoutGenerics = TypeWithoutGenerics(port.Type);
                        // port.IsReversePort = typeWithoutGenerics.EndsWith("B");
                        port.IsReversePort = IsReversePort(port.Type);

                        if (port.IsReversePort)
                        {
                            port.IsInputPort = true;
                            model.AddImplementedPort(port.Type, port.Name, isReversePort: true, description: port.Description); 
                        }
                        else
                        {
                            port.IsInputPort = false;
                            model.AddAcceptedPort(port.Type, port.Name, description: port.Description);
                        }

                        var indexList = new List<int>();

                        if (portSyntaxNode.Declaration.Type is GenericNameSyntax gen)
                        {
                            var portGenerics = gen
                                .DescendantNodesAndSelf().OfType<GenericNameSyntax>()
                                .SelectMany(n => n.TypeArgumentList.Arguments.Select(a => a.ToString()).Where(a => modelGenerics.Contains(a)))
                                .ToHashSet();

                            foreach (var portGeneric in portGenerics)
                            {
                                var index = modelGenerics.IndexOf(portGeneric);
                                if (index != -1) indexList.Add(index);
                            }
                        }

                        model.AddPortGenericIndices(port.Name, indexList);
                    }
                }
                
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set {(isInputPort ? "implemented" : "accepted")} ports in AbstractionModelManager.\nError: {e}");
            }
        }

        public void SetProperties(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            try
            {
                var parser = new CodeParser()
                {
                    AccessLevel = "public"
                };

                var properties = parser
                    .FilterByAccessLevel(parser.GetProperties(classNode), accessLevel: "public")
                    .Select(p => (p as PropertyDeclarationSyntax));

                foreach (var property in properties)
                {
                    if (property == null) continue;

                    var propertyName = property.Identifier.ValueText;

                    model.AddProperty(propertyName, property.Initializer?.Value.ToString() ?? "default", type: property.Type.ToString());

                    if (property.HasLeadingTrivia)
                    {
                        var parsedDocumentation = GetDocumentation(property);
                        model.AddDocumentation(propertyName, parsedDocumentation);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set properties in AbstractionModelManager.\nError: {e}");
            }
        }

        public void SetFields(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            try
            {
                var parser = new CodeParser()
                {
                    AccessLevel = "public"
                };

                var fields = parser
                    .FilterByAccessLevel(parser.GetFields(classNode), accessLevel: "public")
                    .Select(p => (p as FieldDeclarationSyntax).Declaration);

                foreach (var field in fields)
                {
                    var fieldName = field.Variables.First().Identifier.ToString();
                    var fieldValue = field.Variables.First().Initializer?.Value.ToString() ?? "default";
                    var type = field.Type.ToString();

                    model.AddField(fieldName, fieldValue, type);

                    if (field.HasLeadingTrivia)
                    {
                        var parsedDocumentation = GetDocumentation(field);
                        model.AddDocumentation(fieldName, parsedDocumentation);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set fields in AbstractionModelManager.\nError: {e}");
            }
        }

        public void SetDocumentation(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            try
            {
                var documentation = GetDocumentation(classNode);

                // Get port documentation
                var portDocs = new Dictionary<string, string>();
                var lines = documentation.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var candidate = line.Trim();
                    if (Regex.IsMatch(candidate, @"^[0-9]"))
                    {
                        var inv = InverseStringFormat.GetInverseStringFormat(candidate, @"{portIndex}. {portType} {portName}: {portDescription}");

                        if (inv.ContainsKey("portName") && inv.ContainsKey("portDescription") && !string.IsNullOrEmpty(inv["portName"]) && !string.IsNullOrEmpty(inv["portDescription"]))
                        {
                            var port = model.GetPort(inv["portName"].Trim(' ', '\"', '\''));
                            if (port != null) port.Description = inv["portDescription"].Trim();
                        }
                    }
                }

                model.AddDocumentation(documentation);
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set documentation in AbstractionModelManager.\nError: {e}");
            }
        }

        public string GetDocumentation(SyntaxNode node)
        {
            var rawText = node.GetLeadingTrivia().ToString();

            var documentation = ParseDocumentation(rawText);

            return documentation;
        }

        private string ParseDocumentation(string rawText)
        {
            var lines = rawText.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim('/', ' ')).ToList();

            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var cleanedLine = line
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("<summary>", "")
                    .Replace("</summary>", "")
                    .Replace("<para>", "")
                    .Replace("</para>", "")
                    .Replace("<code>", "")
                    .Replace("</code>", "")
                    .Replace("<remarks>", "")
                    .Replace("</remarks>", "");

                if (!string.IsNullOrWhiteSpace(cleanedLine)) sb.AppendLine(cleanedLine);
            }

            var documentation = sb.ToString().Trim('\r', '\n');

            return documentation;
        }

        public void SetConstructorArgs(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            try
            {
                var parser = new CodeParser();

                var ctor = parser.GetConstructors(classNode).FirstOrDefault() as ConstructorDeclarationSyntax;
                if (ctor == null) return;

                var ctorArgs = ctor.ParameterList.Parameters;

                foreach (var arg in ctorArgs)
                {
                    model.AddConstructorArg(arg.Identifier.ToString(), arg.Default?.Value.ToString() ?? "default", type: arg.Type.ToString());
                }
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set constructor args in AbstractionModelManager.\nError: {e}");
            }
        }

        public AbstractionModelManager()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            EventConnector id_8a142e0c2f524b91bfa3af96a6a80c00 = new EventConnector() {  };
            FileBrowser fileBrowser = new FileBrowser() { InstanceName = "fileBrowser", Mode = "Open" };
            FileReader fileReader = new FileReader() { InstanceName = "fileReader" };
            Apply<string, AbstractionModel> id_216e4fddfece493282d795635ba9d1b1 = new Apply<string, AbstractionModel>() { Lambda = filePath => CreateAbstractionModelFromPath(filePath) };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            id_8a142e0c2f524b91bfa3af96a6a80c00.WireTo(fileBrowser, "fanoutList");
            fileBrowser.WireTo(fileReader, "selectedFilePathOutput");
            fileReader.WireTo(id_216e4fddfece493282d795635ba9d1b1, "fileContentOutput");
            // END AUTO-GENERATED WIRING

            _fileBrowser = fileBrowser;
            _fileReader = fileReader;
        }
    }
}

























