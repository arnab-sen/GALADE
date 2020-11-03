using System;
using System.Collections.Generic;
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

        // Private fields
        private FileBrowser _fileBrowser;
        private FolderBrowser _folderBrowser;
        private FileReader _fileReader;
        private List<string> _programmingParadigms = new List<string>() { "IDataFlow", "IEvent", "IUI", "IEventHandler" };
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


        public AbstractionModel CreateAbstractionModel(string code)
        {
            var model = new AbstractionModel();

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

        /// <summary>
        /// Clones an AbstractionModel source into an AbstractionModel destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public void UpdateAbstractionModel(AbstractionModel source, AbstractionModel destination)
        {
            destination.CloneFrom(source);
        }

        public AbstractionModel GetAbstractionModel(string type) => _abstractionModels.ContainsKey(type) ? _abstractionModels[type] : null;
        public List<string> GetAbstractionTypes() => _abstractionModels.Keys.ToList();

        private bool StartMatch(string candidate, IEnumerable<string> set, string prefix = "", string suffix = "")
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

            if (classNode.TypeParameterList != null) type += classNode.TypeParameterList.ToString();

            model.Type = type;
        }

        public void SetAcceptedPorts(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            try
            {
                var parser = new CodeParser()
                {
                    AccessLevel = "private"
                };

                var privateFields = parser.GetFields(classNode);
                var portSyntaxes = privateFields.Where(n =>
                    n is FieldDeclarationSyntax field && 
                        (StartMatch(field.Declaration.Type.ToString(), _programmingParadigms) ||
                        StartMatch(field.Declaration.Type.ToString(), _programmingParadigms, prefix:"List<")))
                    .Select(s => s as FieldDeclarationSyntax);

                var portList = portSyntaxes.Select(s => new Port() { Type = s.Declaration.Type.ToString(), Name = s.Declaration.Variables.First().Identifier.ToString(), IsInputPort = false }).ToList();

                foreach (var port in portList)
                {
                    model.AddAcceptedPort(port.Type, port.Name);
                }
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set accepted ports in AbstractionModelManager.\nError: {e}");
            }
        }

        public void SetImplementedPorts(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            try
            {
                var parser = new CodeParser();

                var implementedList = (parser.GetBaseObjects(classNode).First() as BaseListSyntax).Types.ToList();

                var portList = implementedList.Where(n => StartMatch(n.ToString(), _programmingParadigms))
                    .Select(s => new Port() { Type = s.Type.ToString(), Name = "?" + s.Type.ToString().Replace("<", "_").Replace(">", "_"), IsInputPort = true }).ToList();

                foreach (var port in portList)
                {
                    model.AddImplementedPort(port.Type, port.Name);
                }
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set implemented ports in AbstractionModelManager.\nError: {e}");
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
                    model.AddProperty(property.Identifier.ValueText, property.Initializer?.Value.ToString() ?? "default", type: property.Type.ToString());
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
                var parser = new CodeParser();

                var rawText = classNode.GetLeadingTrivia().ToString();

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
                model.AddDocumentation(documentation);
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to set documentation in AbstractionModelManager.\nError: {e}");
            }
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
            EventConnector id_eb1953a144974727a832f23a86dcc0cb = new EventConnector() {  };
            FileBrowser fileBrowser = new FileBrowser() { InstanceName = "fileBrowser", Mode = "Open" };
            FileReader fileReader = new FileReader() { InstanceName = "fileReader" };
            Apply<string, AbstractionModel> id_2bd6685559f44fcb8055935c12b7e3e5 = new Apply<string, AbstractionModel>() { Lambda = filePath => CreateAbstractionModel(filePath) };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            id_eb1953a144974727a832f23a86dcc0cb.WireTo(fileBrowser, "fanoutList");
            fileBrowser.WireTo(fileReader, "selectedFilePathOutput");
            fileReader.WireTo(id_2bd6685559f44fcb8055935c12b7e3e5, "fileContentOutput");
            // END AUTO-GENERATED WIRING

            _fileBrowser = fileBrowser;
            _fileReader = fileReader;
        }
    }
}






















