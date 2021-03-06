using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using System.Windows;
using Microsoft.Win32;
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
            Orientation = WPF.Orientation.Horizontal,
            Margin = new Thickness(5)
        };

        private Dictionary<string, string> _portSourceFiles = new Dictionary<string, string>();

        // Ports
        private IDataFlow<AbstractionModel> generatedModel;
        private IEvent previewButtonClicked;
        private IEvent createButtonClicked;
        private IUI previewTextDisplay;
        private IUI previewNodeDisplay;
        private IDataFlow<string> csProjPath;


        // Methods

        private void CreateContents()
        {
            var userConfigPanel = new WPF.StackPanel()
            {
                
            };

            var layerDropDownPanel = new WPF.StackPanel()
            {
                Orientation = WPF.Orientation.Horizontal
            };

            layerDropDownPanel.Children.Add(new WPF.Label()
            {
                Content = "Layer:",
                Width = 100
            });

            var layerDropDown = new DropDownMenu()
            {
                Items = new List<string>()
                {
                    "Domain Abstractions",
                    "Story Abstractions"
                },
                CanEdit = false,
                Text = "Domain Abstractions",
                Margin = new Thickness(1),
                Width = 150
            };

            layerDropDownPanel.Children.Add((layerDropDown as IUI).GetWPFElement());

            userConfigPanel.Children.Add(layerDropDownPanel);

            var classNameDropDownPanel = new WPF.StackPanel()
            {
                Orientation = WPF.Orientation.Horizontal
            };

            classNameDropDownPanel.Children.Add(new WPF.Label()
            {
                Content = "Class name:",
                Width = 100
            });

            var classNameTextBox = new WPF.TextBox()
            {
                Text = "",
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                AcceptsTab = false,
                Margin = new Thickness(1)
            };

            classNameDropDownPanel.Children.Add(classNameTextBox);
            userConfigPanel.Children.Add(classNameDropDownPanel);

            // Implemented ports
            userConfigPanel.Children.Add(new WPF.Label()
            {
                Content = "Input ports:"
            });

            var inputPortBundle = new RowBundle();

            userConfigPanel.Children.Add(inputPortBundle);

            var addImplementedPortButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Add Port",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            addImplementedPortButton.Click += (sender, args) => inputPortBundle.AddRow();

            userConfigPanel.Children.Add(addImplementedPortButton);

            // Accepted ports
            userConfigPanel.Children.Add(new WPF.Label()
            {
                Content = "Output ports:"
            });

            var outputPortBundle = new RowBundle();

            userConfigPanel.Children.Add(outputPortBundle);

            var addAcceptedPortButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Add Port",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            addAcceptedPortButton.Click += (sender, args) => outputPortBundle.AddRow();

            userConfigPanel.Children.Add(addAcceptedPortButton);

            var previewButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Preview",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 2, 0)
            };

            List<Tuple<string, string>> inputPortData;
            List<Tuple<string, string>> outputPortData;

            previewButton.Click += (sender, args) =>
            {
                inputPortData = inputPortBundle.GetRowData();
                outputPortData = outputPortBundle.GetRowData();

                var layer = layerDropDown.Text == "Story Abstractions"
                    ? Enums.ALALayer.StoryAbstractions
                    : Enums.ALALayer.DomainAbstractions;

                var model = CreateAbstractionModel(layer, classNameTextBox.Text, inputPortData, outputPortData);
                GeneratedModel = model;

                if (generatedModel != null) generatedModel.Data = GeneratedModel;                
                
                previewButtonClicked?.Execute();
            };

            var getDataButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Add to project",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 2, 0)
            };

            getDataButton.Click += (sender, args) =>
            {
                inputPortData = inputPortBundle.GetRowData();
                outputPortData = outputPortBundle.GetRowData();

                var layer = layerDropDown.Text == "Story Abstractions"
                    ? Enums.ALALayer.StoryAbstractions
                    : Enums.ALALayer.DomainAbstractions;

                var model = CreateAbstractionModel(layer, classNameTextBox.Text, inputPortData, outputPortData);
                GeneratedModel = model;

                if (generatedModel != null) generatedModel.Data = GeneratedModel;

                createButtonClicked?.Execute();
            };

            var findCsProjButton = new WPF.Button()
            {
                Width = 100,
                Height = 20,
                Content = "Find .csproj file",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 2, 0)
            };

            findCsProjButton.Click += (sender, args) =>
            {
                var browser = new OpenFileDialog();
                if (browser.ShowDialog() == true && csProjPath != null)
                {
                    csProjPath.Data = browser.FileName;
                }
            };
            
            var buttonPanel = new WPF.StackPanel()
            {
                Orientation = WPF.Orientation.Horizontal
            };

            buttonPanel.Children.Add(previewButton);
            buttonPanel.Children.Add(getDataButton);
            buttonPanel.Children.Add(findCsProjButton);
            userConfigPanel.Children.Add(buttonPanel);

            _contents.Children.Add(userConfigPanel);

            if (previewTextDisplay != null)
            {
                _contents.Children.Add(previewTextDisplay.GetWPFElement());
            }

            if (previewNodeDisplay != null)
            {
                _contents.Children.Add(previewNodeDisplay.GetWPFElement());
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

        private AbstractionModel CreateAbstractionModel(Enums.ALALayer layer, string type, List<Tuple<string, string>> inputPorts, List<Tuple<string, string>> outputPorts)
        {
            var model = new AbstractionModel
            {
                Layer = layer, 
                FullType = type, 
                Type = type
            };

            foreach (var inputPort in inputPorts)
            {
                if (IsReversePort(inputPort.Item1))
                {
                    model.AddAcceptedPort(inputPort.Item1, inputPort.Item2, isReversePort: true, IsInputPort: true);
                }
                else
                {
                    model.AddImplementedPort(inputPort.Item1, inputPort.Item2, IsInputPort: true);
                }
            }

            foreach (var outputPort in outputPorts)
            {
                if (IsReversePort(outputPort.Item1))
                {
                    model.AddImplementedPort(outputPort.Item1, outputPort.Item2, isReversePort: true, IsInputPort: false);
                }
                else
                {
                    model.AddAcceptedPort(outputPort.Item1, outputPort.Item2, IsInputPort: false);
                }
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

        /// <summary>
        /// A container that can store multiple rows of TextBox pairs.
        /// </summary>
        private class RowBundle : WPF.StackPanel
        {
            private List<Tuple<TextBox, TextBox>> _rows = new List<Tuple<TextBox, TextBox>>();

            public double Width { get; set; } = 200;
            public double Height { get; set; } = 25;

            public RowBundle()
            {

            }

            public void AddRow()
            {
                var panel = new WPF.StackPanel()
                {
                    Orientation = WPF.Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 2)
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
                    AcceptsTab = false,
                    Margin = new Thickness(2, 0, 0, 0)
                };

                var removeRowButton = new WPF.Button()
                {
                    Width = 20,
                    Height = 20,
                    Content = "-",
                    Margin = new Thickness(2)
                };

                removeRowButton.Click += (sender, args) =>
                {
                    var index = Children.IndexOf(panel);
                    _rows.RemoveAt(index);
                    Children.RemoveAt(index);
                };

                panel.Children.Add((typeTextBox as IUI).GetWPFElement());
                panel.Children.Add((nameTextBox as IUI).GetWPFElement());
                panel.Children.Add(removeRowButton);

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