using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using System.Windows;
using WPF = System.Windows.Controls;
using static DomainAbstractions.SourceFileGenerator;

namespace StoryAbstractions
{
    /// <summary>
    /// <para>[Add documentation here]</para>
    /// <para>Ports:</para>
    /// <para></para>
    /// </summary>
    public class AbstractionTemplateCreator : IUI, IDataFlow<List<string>> // ui, programmingParadigmFiles
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private TabContainer _tabContainer;
        private WPF.StackPanel _programmingParadigmTabContents = new WPF.StackPanel();
        private WPF.StackPanel _domainAbstractionTabContents = new WPF.StackPanel();
        private WPF.StackPanel _storyAbstractionTabContents = new WPF.StackPanel();

        private List<RowBundle> _domainAbstractionInputPortBundles = new List<RowBundle>();
        private List<RowBundle> _domainAbstractionOutputPortBundles = new List<RowBundle>();

        private Dictionary<string, string> _portSourceFiles = new Dictionary<string, string>();

        // Ports

        // Methods

        private void CreateProgrammingParadigmTabContents()
        {

        }

        private void CreateDomainAbstractionTabContents()
        {
            // All info should be output to some common data object, maybe an AbstractionModel

            var panel = _domainAbstractionTabContents;

            panel.Children.Add(new WPF.Label()
            {
                Content = "Class name:"
            });

            panel.Children.Add(new WPF.TextBox()
            {
                Text = "",
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            panel.Children.Add(new WPF.Label()
            {
                Content = "Input ports:"
            });

            var inputPortBundle = new RowBundle();
            _domainAbstractionInputPortBundles.Add(inputPortBundle);

            panel.Children.Add(inputPortBundle);

            var addInputPortButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Add Port",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            addInputPortButton.Click += (sender, args) => inputPortBundle.AddRow();

            panel.Children.Add(addInputPortButton);

            var getDataButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Create",
                HorizontalAlignment = HorizontalAlignment.Left
            };


            List<Tuple<string, string>> inputPortData;

            getDataButton.Click += (sender, args) =>
            {
                inputPortData = inputPortBundle.GetRowData();
            };

            panel.Children.Add(getDataButton);

        }

        private AbstractionModel CreateAbstractionModel(string type, List<Tuple<string, string>> inputPorts, List<Tuple<string, string>> outputPorts)
        {
            var model = new AbstractionModel();

            model.Type = type;

            foreach (var inputPort in inputPorts)
            {
                model.AddImplementedPort(inputPort.Item1, inputPort.Item2);
            }

            foreach (var outputPort in outputPorts)
            {
                model.AddImplementedPort(outputPort.Item1, outputPort.Item2);
            }

            return model;
        }

        private void GetPortFiles(List<string> sourceLocations)
        {
            var reader = new FileReader();

            foreach (var sourceLocation in sourceLocations)
            {
                var raw = reader.ReadFile(sourceLocation);
                var interfaceSource = ExtractInterfaces(raw);


                var content = reader.ReadFile(sourceLocation);
                _portSourceFiles[Path.GetFileNameWithoutExtension(sourceLocation)] = content;
            }
        }

        /// <summary>
        /// Extracts interface definitions from a file that may contain multiple.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private Dictionary<string, ParsedInterface> ExtractInterfaces(string sourceFile)
        {
            var extracted = new Dictionary<string, ParsedInterface>();
            var parser = new CodeParser();
            var interfaceDefinitions = parser.ExtractStrings(parser.GetInterfaces(sourceFile));

            foreach (var interfaceDefinition in interfaceDefinitions)
            {
                var parsedInterface = ParseInterface(interfaceDefinition);
            }



            return extracted;
        }

        private ParsedInterface ParseInterface(string source)
        {
            var parsedInterface = new ParsedInterface();
            var parser = new CodeParser();
            var declaration = parser.GetInterfaces(source).First();

            var name = declaration;


            return parsedInterface;
        }


        private void CreateStoryAbstractionTabContents()
        {

        }

        UIElement IUI.GetWPFElement()
        {
            _tabContainer = new TabContainer();

            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR UI
            Tab programmingParadigmsTab = new Tab(title:"Programming Paradigm") {InstanceName="programmingParadigmsTab"}; /* {"IsRoot":false} */
            Tab domainAbstractionsTab = new Tab(title:"Domain Abstraction") {InstanceName="domainAbstractionsTab"}; /* {"IsRoot":false} */
            Tab storyAbstractionsTab = new Tab(title:"Story Abstraction") {InstanceName="storyAbstractionsTab"}; /* {"IsRoot":false} */
            UIFactory id_5cc0ec47a0584bf7bcd8d36eae35294a = new UIFactory() {InstanceName="id_5cc0ec47a0584bf7bcd8d36eae35294a",GetUIElement=() => _programmingParadigmTabContents}; /* {"IsRoot":false} */
            UIFactory id_48fb52366d434827af531605c8e3a898 = new UIFactory() {InstanceName="id_48fb52366d434827af531605c8e3a898",GetUIElement=() => _domainAbstractionTabContents}; /* {"IsRoot":false} */
            UIFactory id_a124ba50dc5e4962a06314662145f106 = new UIFactory() {InstanceName="id_a124ba50dc5e4962a06314662145f106",GetUIElement=() => _storyAbstractionTabContents}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR UI

            // BEGIN AUTO-GENERATED WIRING FOR UI
            _tabContainer.WireTo(programmingParadigmsTab, "childrenTabs"); /* {"SourceType":"TabContainer","SourceIsReference":true,"DestinationType":"Tab","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            _tabContainer.WireTo(domainAbstractionsTab, "childrenTabs"); /* {"SourceType":"TabContainer","SourceIsReference":true,"DestinationType":"Tab","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            _tabContainer.WireTo(storyAbstractionsTab, "childrenTabs"); /* {"SourceType":"TabContainer","SourceIsReference":true,"DestinationType":"Tab","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            programmingParadigmsTab.WireTo(id_5cc0ec47a0584bf7bcd8d36eae35294a, "children"); /* {"SourceType":"Tab","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            domainAbstractionsTab.WireTo(id_48fb52366d434827af531605c8e3a898, "children"); /* {"SourceType":"Tab","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            storyAbstractionsTab.WireTo(id_a124ba50dc5e4962a06314662145f106, "children"); /* {"SourceType":"Tab","SourceIsReference":false,"DestinationType":"UIFactory","DestinationIsReference":false,"Description":"","SourceGenerics":[],"DestinationGenerics":[]} */
            // END AUTO-GENERATED WIRING FOR UI

            CreateProgrammingParadigmTabContents();
            CreateDomainAbstractionTabContents();
            CreateStoryAbstractionTabContents();

            return (_tabContainer as IUI).GetWPFElement();
        }

        List<string> IDataFlow<List<string>>.Data
        {
            get => default;
            set
            {
                _portSourceFiles.Clear();
                GetPortFiles(value);
            }
        }


        public AbstractionTemplateCreator()
        {

        }

        private class RowBundle : WPF.StackPanel
        {
            private List<Tuple<DropDownMenu, TextBox>> _rows = new List<Tuple<DropDownMenu, TextBox>>();

            public List<string> DropDownItems { get; set; } = new List<string>();
            public double Width { get; set; } = 200;
            public double Height { get; set; } = 25;

            public void AddRow()
            {
                var panel = new WPF.StackPanel()
                {
                    Orientation = WPF.Orientation.Horizontal
                };

                var dropDown = new DropDownMenu()
                {
                    Items = DropDownItems,
                    Width = Width / 2,
                    Height = Height
                };

                var textBox = new TextBox()
                {
                    Width = Width / 2,
                    Height = Height
                };

                panel.Children.Add((dropDown as IUI).GetWPFElement());
                panel.Children.Add((textBox as IUI).GetWPFElement());

                Children.Add(panel);

                _rows.Add(Tuple.Create(dropDown, textBox));
            }

            public List<Tuple<string, string>> GetRowData()
            {
                var data = new List<Tuple<string, string>>();

                foreach (var tuple in _rows)
                {
                    data.Add(Tuple.Create(tuple.Item1.Text, tuple.Item2.Text));
                }

                return data;
            }
        }
    }
}