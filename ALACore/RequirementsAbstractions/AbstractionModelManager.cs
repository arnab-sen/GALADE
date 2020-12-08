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

namespace RequirementsAbstractions
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

        private void SetPorts(ClassDeclarationSyntax classNode, AbstractionModel model, bool isInputPort = false)
        {
            try
            {
                if (isInputPort)
                {
                    var parser = new CodeParser();

                    var portNodeList = (parser.GetBaseObjects(classNode).First() as BaseListSyntax)?.Types.ToList();
                    if (portNodeList == null) return;

                    var portSyntaxNodes = portNodeList.Where(n => MatchStartOfString(n.ToString(), ProgrammingParadigms));

                    var modelGenerics = model.GetGenerics();
                    foreach (var portSyntaxNode in portSyntaxNodes)
                    {
                        var port = new Port()
                        {
                            Type = portSyntaxNode.Type.ToString(), 
                            Name = "?" + portSyntaxNode.Type.ToString(), 
                        };

                        // Handle reverse ports (IDataFlowB and IEventB)
                        port.IsReversePort = port.Type.Contains("IDataFlowB") || model.Type.Contains("IEventB");

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

                        // Handle reverse ports (IDataFlowB and IEventB)
                        port.IsReversePort = port.Type.Contains("IDataFlowB") || model.Type.Contains("IEventB");

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
                                .SelectMany(n => n.TypeArgumentList.Arguments.Select(a => a.ToString()))
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
            var lines = rawText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim('/', ' ')).ToList();

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

























