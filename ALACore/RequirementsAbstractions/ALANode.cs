using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;
using Button = DomainAbstractions.Button;
using TextBox = DomainAbstractions.TextBox;
using System.Windows.Forms.VisualStyles;

namespace RequirementsAbstractions
{
    public class ALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Id { get; set; }
        public string Type { get; set; } = "?";
        public string Name { get; set; } = "";
        public List<string> AvailableProgrammingParadigms { get; } = new List<string>();
        public List<string> AvailableDomainAbstractions { get; } = new List<string>();
        public List<string> AvailableRequirementsAbstractions { get; } = new List<string>();
        public Graph Graph { get; set; }
        public List<object> Edges { get; } = new List<object>();
        public Canvas Canvas { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }
        public int MaxInputPorts { get; set; } = 10;
        public int MaxOutputPorts { get; set; } = 10;

        public delegate void SomethingChangedDelegate();
        public delegate void TextChangedDelegate(string text);
        public TextChangedDelegate TypeChanged;

        public SomethingChangedDelegate PositionChanged;
        public Func<Port, Point> GetAttachmentPoint { get; set; }

        // Private fields
        private Box rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);
        private List<Box> _inputPortBoxes = new List<Box>();
        private List<Box> _outputPortBoxes = new List<Box>();

        // Global instances
        private Vertical _inputPortsVert;
        private Vertical _outputPortsVert;

        // Ports

        // Methods

        /// <summary>
        /// Get the currently selected port. If none are selected, then return a default port.
        /// </summary>
        /// <param name="inputPort"></param>
        /// <returns></returns>
        public Box GetSelectedPort(bool inputPort = false)
        {
            if (_selectedPort != null) return _selectedPort;

            List<Box> boxList = inputPort ? _inputPortBoxes : _outputPortBoxes;

            return boxList.FirstOrDefault(box => box.Payload is Port port && port.IsInputPort == inputPort);
        }

        public void UpdateUI()
        {

        }

        /// <summary>
        /// Updates existing ports with the information from a new set of ports,
        /// and returns a list containing the ports that were not updated due to a lack of
        /// existing instantiated Boxes.
        /// </summary>
        /// <param name="newPorts"></param>
        private List<Port> UpdatePorts(IEnumerable<Port> newPorts)
        {
            var notUpdated = new List<Port>();
            int inputIndex = 0;
            int outputIndex = 0;

            foreach (var newPort in newPorts)
            {
                if (newPort.IsInputPort)
                {
                    if (inputIndex < _inputPortBoxes.Count)
                    {
                        var box = _inputPortBoxes[inputIndex];
                        var oldPort = box.Payload as Port;

                        oldPort.Type = newPort.Type;
                        oldPort.Name = newPort.Name;

                        var newText = new Text(newPort.Name)
                        {
                            HorizAlignment = HorizontalAlignment.Center
                        };

                        box.Render.Child = (newText as IUI).GetWPFElement();
                        
                        inputIndex++;
                    }
                    else
                    {
                        notUpdated.Add(newPort);
                    }
                }
                else
                {
                    if (outputIndex < _outputPortBoxes.Count)
                    {
                        var box = _outputPortBoxes[outputIndex];
                        var oldPort = box.Payload as Port;

                        oldPort.Type = newPort.Type;
                        oldPort.Name = newPort.Name;

                        var newText = new Text(newPort.Name)
                        {
                            HorizAlignment = HorizontalAlignment.Center
                        };

                        box.Render.Child = (newText as IUI).GetWPFElement();
                        
                        outputIndex++;
                    }
                    else
                    {
                        notUpdated.Add(newPort);
                    }
                }
            }

            return notUpdated;
        }

        private void SetWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            if (Model == null)
            {
                Model = CreateDummyAbstractionModel();
                AvailableDomainAbstractions.AddRange(new string[100]);
            }

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_5511313ab0fb413e83923fe57c1a76e9 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100, Items = AvailableDomainAbstractions };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.Name, Width = 50 };
            UIFactory id_bb8f03294a9a48af9f9b9a61476b0da3 = new UIFactory(getUIContainer: () =>{_inputPortsVert = new Vertical();_inputPortsVert.Margin = new Thickness(0);return _inputPortsVert;}) {  };
            ConvertToEvent<object> id_f97d556777d64643a49015361cdf3eae = new ConvertToEvent<object>() {  };
            Data<object> id_91f9a7cea4cd49e38b03618fda4ebbe9 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<Port>> id_e3601270579f43fb9438abf1368dd4fb = new Cast<object, IEnumerable<Port>>() {  };
            ForEach<Port> id_a9767fa19a9c4835a2da0b81649c58d6 = new ForEach<Port>() {  };
            Apply<Port, object> setUpPortBox = new Apply<Port, object>() { InstanceName = "setUpPortBox", Lambda = port =>{var box = new Box();box.Payload = port;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = port.ToString()};box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = port.ToString();var text = new Text(text: port.Name);text.HorizAlignment = HorizontalAlignment.Center;box.Render.Child = (text as IUI).GetWPFElement();if (port.IsInputPort){_inputPortsVert.WireTo(box, "children");_inputPortBoxes.Add(box);}else{_outputPortsVert.WireTo(box, "children");_outputPortBoxes.Add(box);}return box;} };
            UIFactory id_15438da4578a49e4b6b653471637e8b4 = new UIFactory(getUIContainer: () =>{_outputPortsVert = new Vertical();_outputPortsVert.Margin = new Thickness(0);return _outputPortsVert;}) {  };
            ApplyAction<object> id_5cefcbf4b75e46bf91bbe56966a9b9fb = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_15fb68eda8ca441a858eb15cf6921395 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_b15238fff64440ec804ca47844fe37d2 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            Apply<object, object> id_b070cd91ad4b46c0ab7f21bc7842baa2 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_2b7971e275204a4eae625fb2d5487799 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_0336dfa3a5594fb38d9703f5e4eeb503 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_92fd3199274045cd8a573747f40020f7 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> addUIEventsToPort = new DataFlowConnector<object>() { InstanceName = "addUIEventsToPort" };
            Apply<object, object> id_e361cd7f2ec04800ae9b88a27dcaa6d9 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_5c4eb3db05fb40c091a0e101a9b9989b = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_47a8bae41b4e43d486c0ffa731245957 = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_6ff4504b89ab457f913b18e9ede60738 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_a066c0eb11464387b77977cea1a0e67a = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_4b693ca7b957451980f22788e3f70c0b = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_4c4a41e48e5a4bcbb95c43ea71c81f21 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_f4ce5414826045d794d5933b88d5ff0b = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_0749057d03cb4e70adac810cb62921bc = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_242968c3413f429986d07db28976afbc = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_ef0bacdf04d94ba4a8bbf8d16b1cae1d = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_751aa48abfe84695b4d9af60c001d97b = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_9e9600ef51cb44d883dce36081d51df8 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_e7b9d6ce2b094563b40839d4cf1cda96 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_afa2f7598f564b9194030d606d160d0e = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_f82ae0c057674cb59b7aac77e03170b5 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            ApplyAction<object> id_26978aa146854436abedda3b62560ed6 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_a44a71df156b459e967cb852d59ad895 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_5500b59da4314a30b69663957b642c25 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_e0b37ded66b44732a4cfb13e8a9b4d89 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_2a1aadba12ab4a08a7e0915eb02d57d9 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift) };
            ApplyAction<object> id_1b0a7b9d2cdb4bea9bfd914fe7468ca9 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            ApplyAction<object> id_e19b126ecea0467a87b4942dfd33f636 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_2fda92ce35e8477c9ba0a508c38f28e3 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            MouseButtonEvent id_e5f23026d915481bb2de15f33627a7af = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<string> id_236b2834f2d34c66a996b4770a534722 = new ApplyAction<string>() { Lambda = input =>{Model.Type = input;TypeChanged?.Invoke(Model.Type);} };
            MouseButtonEvent id_2a78a29c222e4909a8cbad9cc8d347c0 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_35212c037c7e4204b1b146f7c8a38972 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;if (Mouse.Captured?.Equals(render) ?? false) Mouse.Capture(null);} };
            ApplyAction<object> id_8f0907ab847d45bcbac20f4f41b9e66f = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            EventLambda setNodeToolTip = new EventLambda() { InstanceName = "setNodeToolTip", Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            Apply<object, object> id_cabbed7c83a94ee4acd857d386823ef0 = new Apply<object, object>() { Lambda = input =>{var notUpdated = UpdatePorts(input as IEnumerable<Port>);return notUpdated;} };
            ConvertToEvent<object> id_3cd8a050b44f43fcb7a83ee1783c7c7a = new ConvertToEvent<object>() {  };
            Data<object> id_0b86e7f997954d3d84935470ef68289b = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_5511313ab0fb413e83923fe57c1a76e9, "uiLayout");
            rootUI.WireTo(id_242968c3413f429986d07db28976afbc, "eventHandlers");
            rootUI.WireTo(id_751aa48abfe84695b4d9af60c001d97b, "eventHandlers");
            rootUI.WireTo(id_e7b9d6ce2b094563b40839d4cf1cda96, "eventHandlers");
            rootUI.WireTo(id_afa2f7598f564b9194030d606d160d0e, "eventHandlers");
            rootUI.WireTo(id_2a1aadba12ab4a08a7e0915eb02d57d9, "eventHandlers");
            rootUI.WireTo(id_e5f23026d915481bb2de15f33627a7af, "eventHandlers");
            rootUI.WireTo(id_2a78a29c222e4909a8cbad9cc8d347c0, "eventHandlers");
            id_5511313ab0fb413e83923fe57c1a76e9.WireTo(id_bb8f03294a9a48af9f9b9a61476b0da3, "children");
            id_5511313ab0fb413e83923fe57c1a76e9.WireTo(nodeMiddle, "children");
            id_5511313ab0fb413e83923fe57c1a76e9.WireTo(id_15438da4578a49e4b6b653471637e8b4, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            nodeTypeDropDownMenu.WireTo(id_236b2834f2d34c66a996b4770a534722, "selectedItem");
            id_bb8f03294a9a48af9f9b9a61476b0da3.WireTo(id_f97d556777d64643a49015361cdf3eae, "uiInstanceOutput");
            id_f97d556777d64643a49015361cdf3eae.WireTo(id_91f9a7cea4cd49e38b03618fda4ebbe9, "eventOutput");
            id_91f9a7cea4cd49e38b03618fda4ebbe9.WireTo(id_cabbed7c83a94ee4acd857d386823ef0, "dataOutput");
            id_cabbed7c83a94ee4acd857d386823ef0.WireTo(id_e3601270579f43fb9438abf1368dd4fb, "output");
            id_e3601270579f43fb9438abf1368dd4fb.WireTo(id_a9767fa19a9c4835a2da0b81649c58d6, "output");
            id_a9767fa19a9c4835a2da0b81649c58d6.WireTo(setNodeToolTip, "complete");
            id_a9767fa19a9c4835a2da0b81649c58d6.WireTo(setUpPortBox, "elementOutput");
            setUpPortBox.WireTo(id_15fb68eda8ca441a858eb15cf6921395, "output");
            id_15438da4578a49e4b6b653471637e8b4.WireTo(id_3cd8a050b44f43fcb7a83ee1783c7c7a, "uiInstanceOutput");
            id_15fb68eda8ca441a858eb15cf6921395.WireTo(addUIEventsToPort, "fanoutList");
            id_15fb68eda8ca441a858eb15cf6921395.WireTo(id_8f0907ab847d45bcbac20f4f41b9e66f, "fanoutList");
            id_b070cd91ad4b46c0ab7f21bc7842baa2.WireTo(id_2b7971e275204a4eae625fb2d5487799, "output");
            id_2b7971e275204a4eae625fb2d5487799.WireTo(id_5cefcbf4b75e46bf91bbe56966a9b9fb, "wire");
            id_0336dfa3a5594fb38d9703f5e4eeb503.WireTo(id_92fd3199274045cd8a573747f40020f7, "output");
            id_92fd3199274045cd8a573747f40020f7.WireTo(id_b15238fff64440ec804ca47844fe37d2, "wire");
            addUIEventsToPort.WireTo(id_b070cd91ad4b46c0ab7f21bc7842baa2, "fanoutList");
            addUIEventsToPort.WireTo(id_0336dfa3a5594fb38d9703f5e4eeb503, "fanoutList");
            addUIEventsToPort.WireTo(id_e361cd7f2ec04800ae9b88a27dcaa6d9, "fanoutList");
            addUIEventsToPort.WireTo(id_6ff4504b89ab457f913b18e9ede60738, "fanoutList");
            addUIEventsToPort.WireTo(id_4c4a41e48e5a4bcbb95c43ea71c81f21, "fanoutList");
            id_e361cd7f2ec04800ae9b88a27dcaa6d9.WireTo(id_5c4eb3db05fb40c091a0e101a9b9989b, "output");
            id_5c4eb3db05fb40c091a0e101a9b9989b.WireTo(id_47a8bae41b4e43d486c0ffa731245957, "wire");
            id_6ff4504b89ab457f913b18e9ede60738.WireTo(id_a066c0eb11464387b77977cea1a0e67a, "output");
            id_a066c0eb11464387b77977cea1a0e67a.WireTo(id_4b693ca7b957451980f22788e3f70c0b, "wire");
            id_4c4a41e48e5a4bcbb95c43ea71c81f21.WireTo(id_f4ce5414826045d794d5933b88d5ff0b, "output");
            id_f4ce5414826045d794d5933b88d5ff0b.WireTo(id_0749057d03cb4e70adac810cb62921bc, "wire");
            id_242968c3413f429986d07db28976afbc.WireTo(id_ef0bacdf04d94ba4a8bbf8d16b1cae1d, "sourceOutput");
            id_751aa48abfe84695b4d9af60c001d97b.WireTo(id_9e9600ef51cb44d883dce36081d51df8, "sourceOutput");
            id_e7b9d6ce2b094563b40839d4cf1cda96.WireTo(id_a44a71df156b459e967cb852d59ad895, "sourceOutput");
            id_afa2f7598f564b9194030d606d160d0e.WireTo(id_f82ae0c057674cb59b7aac77e03170b5, "sourceOutput");
            id_5500b59da4314a30b69663957b642c25.WireTo(id_26978aa146854436abedda3b62560ed6, "fanoutList");
            id_5500b59da4314a30b69663957b642c25.WireTo(id_e0b37ded66b44732a4cfb13e8a9b4d89, "fanoutList");
            id_5500b59da4314a30b69663957b642c25.WireTo(id_e19b126ecea0467a87b4942dfd33f636, "fanoutList");
            id_5500b59da4314a30b69663957b642c25.WireTo(id_2fda92ce35e8477c9ba0a508c38f28e3, "fanoutList");
            id_2a1aadba12ab4a08a7e0915eb02d57d9.WireTo(id_1b0a7b9d2cdb4bea9bfd914fe7468ca9, "sourceOutput");
            id_e5f23026d915481bb2de15f33627a7af.WireTo(id_5500b59da4314a30b69663957b642c25, "sourceOutput");
            id_2a78a29c222e4909a8cbad9cc8d347c0.WireTo(id_35212c037c7e4204b1b146f7c8a38972, "sourceOutput");
            id_0b86e7f997954d3d84935470ef68289b.WireTo(id_cabbed7c83a94ee4acd857d386823ef0, "dataOutput");
            id_3cd8a050b44f43fcb7a83ee1783c7c7a.WireTo(id_0b86e7f997954d3d84935470ef68289b, "eventOutput");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();

            // Instance mapping
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel()
            {
                Type = "NewNode",
                Name = "defaultName"
            };
            model.AddImplementedPort("Port", "input");
            model.AddAcceptedPort("Port", "output");

            return model;
        }

        public void CreateInternals()
        {
            SetWiring();
        }

        public ALANode()
        {
            Id = Utilities.GetUniqueId();
        }
    }
}


















































































































































































































































































































