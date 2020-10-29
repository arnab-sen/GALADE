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

namespace RequirementsAbstractions
{
    public class ALANode
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Type { get; set; } = "?";
        public string Name { get; set; } = "";
        public List<string> AvailableProgrammingParadigms { get; } = new List<string>();
        public List<string> AvailableDomainAbstractions { get; } = new List<string>();
        public List<string> AvailableRequirementsAbstractions { get; } = new List<string>();
        public Graph Graph { get; set; }
        public Canvas Canvas { get; set; }
        public StateTransition<Enums.DiagramMode> StateTransition { get; set; }
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }
        public List<Box> PortBoxes { get; } = new List<Box>();

        public delegate void SomethingChangedDelegate();

        public SomethingChangedDelegate PositionChanged;

        // Private fields
        private Box rootUI;
        private Box _selectedPort;
        private Point _mousePosInBox = new Point(0, 0);

        // Ports

        // Methods

        /// <summary>
        /// Get the currently selected port, and if none are selected, then return a default port.
        /// </summary>
        /// <param name="inputPort"></param>
        /// <returns></returns>
        public Box GetSelectedPort(bool inputPort = false) => _selectedPort ?? PortBoxes.FirstOrDefault(box => box.Payload is Port port && port.IsInputPort == inputPort);

        private void SetWiring()
        {
            rootUI = new Box()
            {
                Background = Brushes.LightSkyBlue
            };

            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_c2b793c468894535bde4a49b24407d34 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_503cd935abda4a79876d0aff1730a101 = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();inputPortsVert.Margin = new Thickness(0);return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_aedd16854f3a4caa865c6f382cf0c19e = new ConvertToEvent<object>() {  };
            EventConnector id_239731a9f188401eac83328fbcd913ab = new EventConnector() {  };
            Data<object> id_2b0c725fc92649c9ae2f480fcf57dfe8 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_06160e14cb4d43b38625204bcc3834ae = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_2419151f1740410fb1218cbd026b9dd9 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_eefed37962db4d9c9f74d29fba470bcf = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            UIFactory id_f6c2bc76491c47bfb2f879aacbe6d9db = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();outputPortsVert.Margin = new Thickness(0);return outputPortsVert;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_d823355f99154a57a5ad14dd5e27f6bf = new ConvertToEvent<object>() {  };
            EventConnector id_026f6708a4134f00b783af74d77eb9b0 = new EventConnector() {  };
            Data<object> id_e8409e3f8f4f4cf0bd72f7bca04df498 = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_9daf79a7f35e44b9bc2a2045b19644f3 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_2616aec4eb6d4d09ad2e432046cc1f08 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_a69bc39307ba49f3a50caf666452803e = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            ApplyAction<object> id_14e9832e955943108a1552df9c8c43e9 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_5e659495718b457ca02321fc48371807 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_21a521a7b091428cb5159158e3a60447 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_1a998bba214745be9415d7f413fc50d2 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_1a386222248d41749d9d943dd6c04f33 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_5159587401ac4c19a8e1cdfc340077c6 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_070136b1368e408fa08b8ce1bcf8bbd2 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_a66e8176eb844dd9bc43150b1c030816 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_d8d2a99b5eb44b9a96cb11ef2c55f572 = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_de6b128ca6c2429b83f59f7e7ccecea2 = new DataFlowConnector<object>() {  };
            Apply<object, object> id_5007498bb4044b5faf7d7298be1f5ff1 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_42d9d73486b64a758e8d5201731ec237 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_3d6d0d1ca51644309b0141aee8949a2e = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_6547d8460625434cbe46a36c6ed266dc = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_a52cc24b38c141878bb1323c3aae2c95 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_0c4c2f5e3cda4a459a287d0bb155fd43 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_a3480d499f87481f904caabd44bed531 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_bb302aa3c9cf4e708640dd0c8a4a2360 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_08ba71689fac46d3b92e0dc699821c92 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_10961023bc1642fe9a46078da171551b = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_4802acd0930d450d85234629e8f24efd = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_6cff00d11dd64f7e80402589bcee32b8 = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_f95e546e9f074540aeb5a2f1820f14ba = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_72417b273f2742b5a90051a28183d8b1 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_f7055858b66e44abac3f6de5f695370c = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_c97347599bb54db6b2a67d9f586b314b = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_2964349cad5041a3aaa5c79bc35a9ee7 = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_d83bf7f8871741ea8c8fa7162a0313bd = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_8a52698fa0a7435daa24fe2516cdbf3f = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_8ed128b5461b4dc9ada374fc29502e97 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_4d99617192994b9b8571500906fc3e84 = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_a1b6dce6e8ea4882990ae91b021540f0 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed };
            ApplyAction<object> id_37ba2cf20f6742a38d3f4067587cffa5 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            MouseButtonEvent id_084d665142cc4c5690c8f5a2a4880483 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_1df8948d96d04d8a95d79ed76eaa7800 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured == (input as Box).Render) Mouse.Capture(null);} };
            ApplyAction<object> id_58751740284e42fe8ba82e97e4db933c = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_6aaf8809a7c04f99b52740df63d05d67 = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_c2b793c468894535bde4a49b24407d34, "uiLayout");
            rootUI.WireTo(id_10961023bc1642fe9a46078da171551b, "eventHandlers");
            rootUI.WireTo(id_6cff00d11dd64f7e80402589bcee32b8, "eventHandlers");
            rootUI.WireTo(id_72417b273f2742b5a90051a28183d8b1, "eventHandlers");
            rootUI.WireTo(id_f7055858b66e44abac3f6de5f695370c, "eventHandlers");
            rootUI.WireTo(id_2964349cad5041a3aaa5c79bc35a9ee7, "eventHandlers");
            rootUI.WireTo(id_a1b6dce6e8ea4882990ae91b021540f0, "eventHandlers");
            rootUI.WireTo(id_084d665142cc4c5690c8f5a2a4880483, "eventHandlers");
            id_c2b793c468894535bde4a49b24407d34.WireTo(id_503cd935abda4a79876d0aff1730a101, "children");
            id_c2b793c468894535bde4a49b24407d34.WireTo(nodeMiddle, "children");
            id_c2b793c468894535bde4a49b24407d34.WireTo(id_f6c2bc76491c47bfb2f879aacbe6d9db, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_503cd935abda4a79876d0aff1730a101.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_aedd16854f3a4caa865c6f382cf0c19e, "fanoutList");
            id_aedd16854f3a4caa865c6f382cf0c19e.WireTo(id_239731a9f188401eac83328fbcd913ab, "eventOutput");
            id_239731a9f188401eac83328fbcd913ab.WireTo(id_2b0c725fc92649c9ae2f480fcf57dfe8, "fanoutList");
            id_2b0c725fc92649c9ae2f480fcf57dfe8.WireTo(id_06160e14cb4d43b38625204bcc3834ae, "dataOutput");
            id_06160e14cb4d43b38625204bcc3834ae.WireTo(id_2419151f1740410fb1218cbd026b9dd9, "output");
            id_2419151f1740410fb1218cbd026b9dd9.WireTo(id_eefed37962db4d9c9f74d29fba470bcf, "elementOutput");
            id_eefed37962db4d9c9f74d29fba470bcf.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_5e659495718b457ca02321fc48371807, "output");
            id_f6c2bc76491c47bfb2f879aacbe6d9db.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_d823355f99154a57a5ad14dd5e27f6bf, "fanoutList");
            id_d823355f99154a57a5ad14dd5e27f6bf.WireTo(id_026f6708a4134f00b783af74d77eb9b0, "eventOutput");
            id_026f6708a4134f00b783af74d77eb9b0.WireTo(id_e8409e3f8f4f4cf0bd72f7bca04df498, "fanoutList");
            id_e8409e3f8f4f4cf0bd72f7bca04df498.WireTo(id_9daf79a7f35e44b9bc2a2045b19644f3, "dataOutput");
            id_9daf79a7f35e44b9bc2a2045b19644f3.WireTo(id_2616aec4eb6d4d09ad2e432046cc1f08, "output");
            id_2616aec4eb6d4d09ad2e432046cc1f08.WireTo(id_a69bc39307ba49f3a50caf666452803e, "elementOutput");
            id_a69bc39307ba49f3a50caf666452803e.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_5e659495718b457ca02321fc48371807, "output");
            id_5e659495718b457ca02321fc48371807.WireTo(id_d8d2a99b5eb44b9a96cb11ef2c55f572, "fanoutList");
            id_5e659495718b457ca02321fc48371807.WireTo(id_de6b128ca6c2429b83f59f7e7ccecea2, "fanoutList");
            id_1a386222248d41749d9d943dd6c04f33.WireTo(id_5159587401ac4c19a8e1cdfc340077c6, "output");
            id_5159587401ac4c19a8e1cdfc340077c6.WireTo(id_14e9832e955943108a1552df9c8c43e9, "wire");
            id_070136b1368e408fa08b8ce1bcf8bbd2.WireTo(id_a66e8176eb844dd9bc43150b1c030816, "output");
            id_a66e8176eb844dd9bc43150b1c030816.WireTo(id_21a521a7b091428cb5159158e3a60447, "wire");
            id_d8d2a99b5eb44b9a96cb11ef2c55f572.WireTo(id_1a386222248d41749d9d943dd6c04f33, "fanoutList");
            id_d8d2a99b5eb44b9a96cb11ef2c55f572.WireTo(id_070136b1368e408fa08b8ce1bcf8bbd2, "fanoutList");
            id_d8d2a99b5eb44b9a96cb11ef2c55f572.WireTo(id_5007498bb4044b5faf7d7298be1f5ff1, "fanoutList");
            id_d8d2a99b5eb44b9a96cb11ef2c55f572.WireTo(id_6547d8460625434cbe46a36c6ed266dc, "fanoutList");
            id_d8d2a99b5eb44b9a96cb11ef2c55f572.WireTo(id_a3480d499f87481f904caabd44bed531, "fanoutList");
            id_de6b128ca6c2429b83f59f7e7ccecea2.WireTo(id_1a998bba214745be9415d7f413fc50d2, "fanoutList");
            id_5007498bb4044b5faf7d7298be1f5ff1.WireTo(id_42d9d73486b64a758e8d5201731ec237, "output");
            id_42d9d73486b64a758e8d5201731ec237.WireTo(id_3d6d0d1ca51644309b0141aee8949a2e, "wire");
            id_6547d8460625434cbe46a36c6ed266dc.WireTo(id_a52cc24b38c141878bb1323c3aae2c95, "output");
            id_a52cc24b38c141878bb1323c3aae2c95.WireTo(id_0c4c2f5e3cda4a459a287d0bb155fd43, "wire");
            id_a3480d499f87481f904caabd44bed531.WireTo(id_bb302aa3c9cf4e708640dd0c8a4a2360, "output");
            id_bb302aa3c9cf4e708640dd0c8a4a2360.WireTo(id_08ba71689fac46d3b92e0dc699821c92, "wire");
            id_10961023bc1642fe9a46078da171551b.WireTo(id_4802acd0930d450d85234629e8f24efd, "sourceOutput");
            id_6cff00d11dd64f7e80402589bcee32b8.WireTo(id_f95e546e9f074540aeb5a2f1820f14ba, "sourceOutput");
            id_72417b273f2742b5a90051a28183d8b1.WireTo(id_8a52698fa0a7435daa24fe2516cdbf3f, "sourceOutput");
            id_f7055858b66e44abac3f6de5f695370c.WireTo(id_c97347599bb54db6b2a67d9f586b314b, "sourceOutput");
            id_2964349cad5041a3aaa5c79bc35a9ee7.WireTo(id_8ed128b5461b4dc9ada374fc29502e97, "sourceOutput");
            id_8ed128b5461b4dc9ada374fc29502e97.WireTo(id_d83bf7f8871741ea8c8fa7162a0313bd, "fanoutList");
            id_8ed128b5461b4dc9ada374fc29502e97.WireTo(id_4d99617192994b9b8571500906fc3e84, "fanoutList");
            id_8ed128b5461b4dc9ada374fc29502e97.WireTo(id_58751740284e42fe8ba82e97e4db933c, "fanoutList");
            id_8ed128b5461b4dc9ada374fc29502e97.WireTo(id_6aaf8809a7c04f99b52740df63d05d67, "fanoutList");
            id_a1b6dce6e8ea4882990ae91b021540f0.WireTo(id_37ba2cf20f6742a38d3f4067587cffa5, "sourceOutput");
            id_084d665142cc4c5690c8f5a2a4880483.WireTo(id_1df8948d96d04d8a95d79ed76eaa7800, "sourceOutput");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel();
            model.AddImplementedPort("Port", "input");
            model.AddAcceptedPort("Port", "output");
            model.AddProperty("Type", "NewNode");
            model.AddProperty("Name", "");

            return model;
        }

        public void CreateInternals()
        {
            SetWiring();
        }

        public ALANode()
        {

        }
    }
}
















































































































































































































