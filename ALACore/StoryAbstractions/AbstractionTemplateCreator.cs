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
        public AbstractionModel GeneratedModel { get; private set; }

        // Private fields
        private WPF.StackPanel _contents = new WPF.StackPanel()
        {
            Margin = new Thickness(5)
        };

        private List<RowBundle> _domainAbstractionInputPortBundles = new List<RowBundle>();
        private List<RowBundle> _domainAbstractionOutputPortBundles = new List<RowBundle>();

        private Dictionary<string, string> _portSourceFiles = new Dictionary<string, string>();

        // Ports
        private IDataFlow<AbstractionModel> generatedModel;


        // Methods

        private void CreateContents()
        {
            // All info should be output to some common data object, maybe an AbstractionModel

            var panel = _contents;

            panel.Children.Add(new WPF.Label()
            {
                Content = "Class name:"
            });


            var classNameTextBox = new WPF.TextBox()
            {
                Text = "",
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                AcceptsTab = false
            };

            panel.Children.Add(classNameTextBox);

            // Implemented ports
            panel.Children.Add(new WPF.Label()
            {
                Content = "Implemented ports:"
            });

            var implementedPortBundle = new RowBundle();
            _domainAbstractionInputPortBundles.Add(implementedPortBundle);

            panel.Children.Add(implementedPortBundle);

            var addImplementedPortButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Add Port",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            addImplementedPortButton.Click += (sender, args) => implementedPortBundle.AddRow();

            panel.Children.Add(addImplementedPortButton);


            // Accepted ports
            panel.Children.Add(new WPF.Label()
            {
                Content = "Accepted ports:"
            });

            var acceptedPortBundle = new RowBundle();
            _domainAbstractionInputPortBundles.Add(acceptedPortBundle);

            panel.Children.Add(acceptedPortBundle);

            var addAcceptedPortButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Add Port",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            addAcceptedPortButton.Click += (sender, args) => acceptedPortBundle.AddRow();

            panel.Children.Add(addAcceptedPortButton);

            var getDataButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Create",
                HorizontalAlignment = HorizontalAlignment.Left
            };


            List<Tuple<string, string>> implementedPortData;
            List<Tuple<string, string>> acceptedPortData;

            getDataButton.Click += (sender, args) =>
            {
                implementedPortData = implementedPortBundle.GetRowData();
                acceptedPortData = acceptedPortBundle.GetRowData();

                var model = CreateAbstractionModel(classNameTextBox.Text, implementedPortData, acceptedPortData);
                GeneratedModel = model;

                if (generatedModel != null) generatedModel.Data = GeneratedModel;
            };

            panel.Children.Add(getDataButton);

        }

        private AbstractionModel CreateAbstractionModel(string type, List<Tuple<string, string>> implementedPorts, List<Tuple<string, string>> acceptedPorts)
        {
            var model = new AbstractionModel();

            model.FullType = type;
            model.Type = type;

            foreach (var inputPort in implementedPorts)
            {
                model.AddImplementedPort(inputPort.Item1, inputPort.Item2);
            }

            foreach (var outputPort in acceptedPorts)
            {
                model.AddAcceptedPort(outputPort.Item1, outputPort.Item2);
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


        UIElement IUI.GetWPFElement()
        {
            // BEGIN AUTO-GENERATED INSTANTIATIONS FOR UI
            UIFactory windowContents = new UIFactory() {InstanceName="windowContents",GetUIElement=() => _contents}; /* {"IsRoot":false} */
            // END AUTO-GENERATED INSTANTIATIONS FOR UI

            // BEGIN AUTO-GENERATED WIRING FOR UI
            // END AUTO-GENERATED WIRING FOR UI

            CreateContents();

            return (windowContents as IUI).GetWPFElement();
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
            private List<Tuple<TextBox, TextBox>> _rows = new List<Tuple<TextBox, TextBox>>();

            public double Width { get; set; } = 200;
            public double Height { get; set; } = 25;

            public void AddRow()
            {
                var panel = new WPF.StackPanel()
                {
                    Orientation = WPF.Orientation.Horizontal
                };


                var typeTextBox = new TextBox()
                {
                    Width = Width / 2,
                    Height = Height,
                    AcceptsTab = false
                };

                var nameTextBox = new TextBox()
                {
                    Width = Width / 2,
                    Height = Height,
                    AcceptsTab = false
                };

                panel.Children.Add((typeTextBox as IUI).GetWPFElement());
                panel.Children.Add((nameTextBox as IUI).GetWPFElement());

                Children.Add(panel);

                _rows.Add(Tuple.Create(typeTextBox, nameTextBox));
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