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
            var className = classNode.Identifier.ValueText;

            model.Type = className;

            SetImplementedPorts(classNode, model);
            SetAcceptedPorts(classNode, model);
            SetProperties(classNode, model);
            SetDocumentation(classNode, model);

            return model;
        }

        private bool StartMatch(string candidate, IEnumerable<string> set)
        {
            bool matches = false;

            foreach (var str in set)
            {
                if (candidate.StartsWith(str))
                {
                    matches = true;
                    break;
                }
            }

            return matches;
        }

        public void SetAcceptedPorts(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            var parser = new CodeParser()
            {
                AccessLevel = "private"
            };

            var privateFields = parser.GetFields(classNode);
            var portSyntaxes = privateFields.Where(n =>
                n is FieldDeclarationSyntax field && StartMatch(field.Declaration.Type.ToString(), _programmingParadigms)).Select(s => s as FieldDeclarationSyntax);

            var portList = portSyntaxes.Select(s => new Port() { Type = s.Declaration.Type.ToString(), Name = s.Declaration.Variables.First().ToString(), IsInputPort = false }).ToList();

            foreach (var port in portList)
            {
                model.AddAcceptedPort(port.Type, port.Name);
            }
        }

        public void SetImplementedPorts(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            var parser = new CodeParser();

            var implementedList = (parser.GetBaseObjects(classNode).First() as BaseListSyntax).Types.ToList();

            var portList = implementedList.Where(n => StartMatch(n.ToString(), _programmingParadigms))
                .Select(s => new Port() { Type = s.Type.ToString(), Name = "?" + s.Type.ToString(), IsInputPort = true}).ToList();

            foreach (var port in portList)
            {
                model.AddImplementedPort(port.Type, port.Name);
            }
        }

        public void SetProperties(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            var parser = new CodeParser()
            {
                AccessLevel = "public"
            };

            var properties = parser.FilterByAccessLevel(parser.GetProperties(classNode), accessLevel: "public").Select(p => (p as PropertyDeclarationSyntax));

            foreach (var property in properties)
            {
                model.AddProperty(property.Identifier.ValueText, property.Initializer?.Value.ToString() ?? "default");
            }
        }

        public void SetDocumentation(ClassDeclarationSyntax classNode, AbstractionModel model)
        {
            var parser = new CodeParser();

            var rawText = classNode.GetLeadingTrivia().ToString();

            var lines = rawText.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim('/', ' ')).ToList();

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

        public AbstractionModelManager()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS
            EventConnector id_4cfdb9dc71404071bb4968f219f5330b = new EventConnector() {  };
            FileBrowser fileBrowser = new FileBrowser() { InstanceName = "fileBrowser", Mode = "Open" };
            FileReader fileReader = new FileReader() { InstanceName = "fileReader" };
            Apply<string, AbstractionModel> id_12f764aba7ab45c7bc5aa5bb64ff4737 = new Apply<string, AbstractionModel>() { Lambda = CreateAbstractionModel };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            id_4cfdb9dc71404071bb4968f219f5330b.WireTo(fileBrowser, "fanoutList");
            fileBrowser.WireTo(fileReader, "selectedFilePathOutput");
            fileReader.WireTo(id_12f764aba7ab45c7bc5aa5bb64ff4737, "fileContentOutput");
            // END AUTO-GENERATED WIRING

            _fileBrowser = fileBrowser;
            _fileReader = fileReader;
        }
    }
}




















