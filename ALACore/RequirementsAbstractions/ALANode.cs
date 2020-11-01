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
        public List<Box> PortBoxes { get; } = new List<Box>();

        public delegate void SomethingChangedDelegate();

        public SomethingChangedDelegate PositionChanged;
        public Func<Port, Point> GetAttachmentPoint { get; set; }

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

            if (Model == null) Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_353509dd183e4ce987b73df859c82c31 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle", Margin = new Thickness(1, 0, 1, 0) };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.Type, Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.Name, Width = 50 };
            UIFactory id_0472b3a1b70247d38b4dccbe38aa40e0 = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();inputPortsVert.Margin = new Thickness(0);return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_ed6daba2ec0c4c6b9374275e31704d4c = new ConvertToEvent<object>() {  };
            EventConnector id_52a1c05107f44ca994df8bbb87fdacb3 = new EventConnector() {  };
            Data<object> id_33c6cfd0055a4ab6bfe092fba44a1ca4 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_b428e1f9c5d64392ac64a579b8c5dd00 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_166038de576d427cb40f3516f21d12b6 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_d4a201c0755e49a58427744ebf7a6763 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            UIFactory id_3dca2721c00b4138ab790767114046ca = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();outputPortsVert.Margin = new Thickness(0);return outputPortsVert;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_860408f6dd674d22aabb2de74ae5ae51 = new ConvertToEvent<object>() {  };
            EventConnector id_7aab2768a9c0415a91330b8a8480e3df = new EventConnector() {  };
            Data<object> id_cc676b51ba9546e598fe1f0ce852fc66 = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_a596b2a5d327495d9a34e0e0b34a9458 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_7556eeff4d3643c78d6b863be06d7d41 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_77c97ee1add34c4fb9a560fa02eec367 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;box.BorderThickness = new Thickness(2);var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");PortBoxes.Add(box);return box;} };
            ApplyAction<object> id_5f3be95452034b73997d3c9643e8b008 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_043e7bcb31a64159807f74a868191040 = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_f39c09a04e97437a848b65863c74da5a = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_cd1bc013dc244ab0a96ac4ec76d5ac5c = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_11f45ac5805146dc850172308668c395 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_2055942f0b6646d2b77cd7176fe5096e = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_003992dfde004dfe8c4fd38e9022d715 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_84281db3dd0643cfae033fc9200ede20 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            DataFlowConnector<object> id_35715caf18174ff3b5a94e8ad86e0f55 = new DataFlowConnector<object>() {  };
            DataFlowConnector<object> id_de1fa87b54774a9aba22fb5edd68b155 = new DataFlowConnector<object>() {  };
            Apply<object, object> id_5ad282cdf6eb4ee1a4d4eb4fdf504ac4 = new Apply<object, object>() { Lambda = input =>{var mouseButtonEvent = new MouseButtonEvent("MouseDown");mouseButtonEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseButtonEvent, "eventHandlers");return mouseButtonEvent;} };
            DynamicWiring<IDataFlow<object>> id_6c41f4903c834f2bb9531c767cd5526d = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_87b571097aa34e5b856bf9cbeabbb36c = new ApplyAction<object>() { Lambda = input =>{_selectedPort = input as Box;_selectedPort.Render.Focus();} };
            Apply<object, object> id_c0b9a9d6ad714fec8bd294acc0f0f25d = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("GotFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_c60a9e6567f34eb8965c7cd19d62c1ca = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_f994e06965f6438980b437dc89c13d73 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.SandyBrown;} };
            Apply<object, object> id_1518b80e5a124570b536a7721d431fe3 = new Apply<object, object>() { Lambda = input =>{var routedEvent = new RoutedEventSubscriber("LostFocus");routedEvent.ExtractSender = source => (source as Box).Render;input.WireTo(routedEvent, "eventHandlers");return routedEvent;} };
            DynamicWiring<IDataFlow<object>> id_c99c2300003c4039863820c5469c5265 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            ApplyAction<object> id_48afdc12457543549ced6e89a5c6fb7c = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.Background = Brushes.White;} };
            MouseEvent id_af34d72f4c56450b8ba6f5df6638cdb2 = new MouseEvent(eventName: "MouseEnter") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_220ec2d91bc441a2b05c3a26710341ae = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            MouseEvent id_d0d0e50c56ed4d5c8af41af5f52af11f = new MouseEvent(eventName: "MouseLeave") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_38d73fe77688482bb1cd9c0179ec0262 = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) (input as Box).Background = Brushes.LightSkyBlue;} };
            RoutedEventSubscriber id_bcc0907eecca4144b7d1ae9f0317bc83 = new RoutedEventSubscriber(eventName: "GotFocus") { ExtractSender = source => (source as Box).Render };
            RoutedEventSubscriber id_df2b3b1510fd4a19a3edf133bd1ee0ca = new RoutedEventSubscriber(eventName: "LostFocus") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_42ec2980e40945f9a74a41c037ba92f3 = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.LightSkyBlue;} };
            MouseButtonEvent id_701bec1a8c2540b4b5eae91eea4733cc = new MouseButtonEvent(eventName: "MouseLeftButtonDown") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_b77709e411bd4616a0011ed7dbb538af = new ApplyAction<object>() { Lambda = input =>{var ui = (input as Box).Render;if (!ui.IsKeyboardFocusWithin) ui.Focus();} };
            ApplyAction<object> id_433a13d054ab4484be384a3ba22b3afe = new ApplyAction<object>() { Lambda = input =>{(input as Box).Background = Brushes.Aquamarine;} };
            DataFlowConnector<object> id_f012ff5b1a7449898cddcfb48d2041ec = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_e5dbefe2a26049e69a193e917d4a851e = new ApplyAction<object>() { Lambda = input =>{StateTransition.Update(Enums.DiagramMode.IdleSelected);} };
            MouseEvent id_b47431f47f8e4e09b40f9e9e914e5594 = new MouseEvent(eventName: "MouseMove") { ExtractSender = source => (source as Box).Render, Condition = args => Mouse.LeftButton == MouseButtonState.Pressed };
            ApplyAction<object> id_703445ea5fb94d7e995fa3811c4640dc = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;var mousePos = Mouse.GetPosition(Canvas);var oldPosition = new Point(Canvas.GetLeft(render), Canvas.GetTop(render));Canvas.SetLeft(render, mousePos.X - _mousePosInBox.X);Canvas.SetTop(render, mousePos.Y - _mousePosInBox.Y);PositionChanged?.Invoke();} };
            MouseButtonEvent id_24494b8b4c0d4e07bfc670b28c397c20 = new MouseButtonEvent(eventName: "MouseLeftButtonUp") { ExtractSender = source => (source as Box).Render };
            ApplyAction<object> id_b340bcc2ab47409dbbb6685f3cf072f8 = new ApplyAction<object>() { Lambda = input =>{if (Mouse.Captured == (input as Box).Render) Mouse.Capture(null);} };
            ApplyAction<object> id_62b700698cb845e59b9df043e5055623 = new ApplyAction<object>() { Lambda = input =>{var render = (input as Box).Render;_mousePosInBox = Mouse.GetPosition(render);Mouse.Capture(render);} };
            ApplyAction<object> id_5f381b56b6814af2be8d0e9ea9bf26df = new ApplyAction<object>() { Lambda = input =>{Graph.Set("SelectedNode", this);} };
            EventLambda id_c6cd63c78f484af58436307a00897fe8 = new EventLambda() { Lambda = () => {var toolTipLabel = new System.Windows.Controls.Label() { Content = Model.GetDocumentation() };rootUI.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };rootUI.Render.MouseEnter += (sender, args) => toolTipLabel.Content = Model.GetDocumentation();} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_353509dd183e4ce987b73df859c82c31, "uiLayout");
            rootUI.WireTo(id_af34d72f4c56450b8ba6f5df6638cdb2, "eventHandlers");
            rootUI.WireTo(id_d0d0e50c56ed4d5c8af41af5f52af11f, "eventHandlers");
            rootUI.WireTo(id_bcc0907eecca4144b7d1ae9f0317bc83, "eventHandlers");
            rootUI.WireTo(id_df2b3b1510fd4a19a3edf133bd1ee0ca, "eventHandlers");
            rootUI.WireTo(id_701bec1a8c2540b4b5eae91eea4733cc, "eventHandlers");
            rootUI.WireTo(id_b47431f47f8e4e09b40f9e9e914e5594, "eventHandlers");
            rootUI.WireTo(id_24494b8b4c0d4e07bfc670b28c397c20, "eventHandlers");
            id_353509dd183e4ce987b73df859c82c31.WireTo(id_0472b3a1b70247d38b4dccbe38aa40e0, "children");
            id_353509dd183e4ce987b73df859c82c31.WireTo(nodeMiddle, "children");
            id_353509dd183e4ce987b73df859c82c31.WireTo(id_3dca2721c00b4138ab790767114046ca, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_0472b3a1b70247d38b4dccbe38aa40e0.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_ed6daba2ec0c4c6b9374275e31704d4c, "fanoutList");
            id_ed6daba2ec0c4c6b9374275e31704d4c.WireTo(id_52a1c05107f44ca994df8bbb87fdacb3, "eventOutput");
            id_52a1c05107f44ca994df8bbb87fdacb3.WireTo(id_33c6cfd0055a4ab6bfe092fba44a1ca4, "fanoutList");
            id_33c6cfd0055a4ab6bfe092fba44a1ca4.WireTo(id_b428e1f9c5d64392ac64a579b8c5dd00, "dataOutput");
            id_b428e1f9c5d64392ac64a579b8c5dd00.WireTo(id_166038de576d427cb40f3516f21d12b6, "output");
            id_166038de576d427cb40f3516f21d12b6.WireTo(id_d4a201c0755e49a58427744ebf7a6763, "elementOutput");
            id_166038de576d427cb40f3516f21d12b6.WireTo(id_c6cd63c78f484af58436307a00897fe8, "complete");
            id_d4a201c0755e49a58427744ebf7a6763.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_043e7bcb31a64159807f74a868191040, "output");
            id_3dca2721c00b4138ab790767114046ca.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_860408f6dd674d22aabb2de74ae5ae51, "fanoutList");
            id_860408f6dd674d22aabb2de74ae5ae51.WireTo(id_7aab2768a9c0415a91330b8a8480e3df, "eventOutput");
            id_7aab2768a9c0415a91330b8a8480e3df.WireTo(id_cc676b51ba9546e598fe1f0ce852fc66, "fanoutList");
            id_cc676b51ba9546e598fe1f0ce852fc66.WireTo(id_a596b2a5d327495d9a34e0e0b34a9458, "dataOutput");
            id_a596b2a5d327495d9a34e0e0b34a9458.WireTo(id_7556eeff4d3643c78d6b863be06d7d41, "output");
            id_7556eeff4d3643c78d6b863be06d7d41.WireTo(id_77c97ee1add34c4fb9a560fa02eec367, "elementOutput");
            id_77c97ee1add34c4fb9a560fa02eec367.WireTo(setUpOutputPortBox, "output");
            setUpOutputPortBox.WireTo(id_043e7bcb31a64159807f74a868191040, "output");
            id_043e7bcb31a64159807f74a868191040.WireTo(id_35715caf18174ff3b5a94e8ad86e0f55, "fanoutList");
            id_043e7bcb31a64159807f74a868191040.WireTo(id_de1fa87b54774a9aba22fb5edd68b155, "fanoutList");
            id_11f45ac5805146dc850172308668c395.WireTo(id_2055942f0b6646d2b77cd7176fe5096e, "output");
            id_2055942f0b6646d2b77cd7176fe5096e.WireTo(id_5f3be95452034b73997d3c9643e8b008, "wire");
            id_003992dfde004dfe8c4fd38e9022d715.WireTo(id_84281db3dd0643cfae033fc9200ede20, "output");
            id_84281db3dd0643cfae033fc9200ede20.WireTo(id_f39c09a04e97437a848b65863c74da5a, "wire");
            id_35715caf18174ff3b5a94e8ad86e0f55.WireTo(id_11f45ac5805146dc850172308668c395, "fanoutList");
            id_35715caf18174ff3b5a94e8ad86e0f55.WireTo(id_003992dfde004dfe8c4fd38e9022d715, "fanoutList");
            id_35715caf18174ff3b5a94e8ad86e0f55.WireTo(id_5ad282cdf6eb4ee1a4d4eb4fdf504ac4, "fanoutList");
            id_35715caf18174ff3b5a94e8ad86e0f55.WireTo(id_c0b9a9d6ad714fec8bd294acc0f0f25d, "fanoutList");
            id_35715caf18174ff3b5a94e8ad86e0f55.WireTo(id_1518b80e5a124570b536a7721d431fe3, "fanoutList");
            id_de1fa87b54774a9aba22fb5edd68b155.WireTo(id_cd1bc013dc244ab0a96ac4ec76d5ac5c, "fanoutList");
            id_5ad282cdf6eb4ee1a4d4eb4fdf504ac4.WireTo(id_6c41f4903c834f2bb9531c767cd5526d, "output");
            id_6c41f4903c834f2bb9531c767cd5526d.WireTo(id_87b571097aa34e5b856bf9cbeabbb36c, "wire");
            id_c0b9a9d6ad714fec8bd294acc0f0f25d.WireTo(id_c60a9e6567f34eb8965c7cd19d62c1ca, "output");
            id_c60a9e6567f34eb8965c7cd19d62c1ca.WireTo(id_f994e06965f6438980b437dc89c13d73, "wire");
            id_1518b80e5a124570b536a7721d431fe3.WireTo(id_c99c2300003c4039863820c5469c5265, "output");
            id_c99c2300003c4039863820c5469c5265.WireTo(id_48afdc12457543549ced6e89a5c6fb7c, "wire");
            id_af34d72f4c56450b8ba6f5df6638cdb2.WireTo(id_220ec2d91bc441a2b05c3a26710341ae, "sourceOutput");
            id_d0d0e50c56ed4d5c8af41af5f52af11f.WireTo(id_38d73fe77688482bb1cd9c0179ec0262, "sourceOutput");
            id_bcc0907eecca4144b7d1ae9f0317bc83.WireTo(id_433a13d054ab4484be384a3ba22b3afe, "sourceOutput");
            id_df2b3b1510fd4a19a3edf133bd1ee0ca.WireTo(id_42ec2980e40945f9a74a41c037ba92f3, "sourceOutput");
            id_701bec1a8c2540b4b5eae91eea4733cc.WireTo(id_f012ff5b1a7449898cddcfb48d2041ec, "sourceOutput");
            id_f012ff5b1a7449898cddcfb48d2041ec.WireTo(id_b77709e411bd4616a0011ed7dbb538af, "fanoutList");
            id_f012ff5b1a7449898cddcfb48d2041ec.WireTo(id_e5dbefe2a26049e69a193e917d4a851e, "fanoutList");
            id_f012ff5b1a7449898cddcfb48d2041ec.WireTo(id_62b700698cb845e59b9df043e5055623, "fanoutList");
            id_f012ff5b1a7449898cddcfb48d2041ec.WireTo(id_5f381b56b6814af2be8d0e9ea9bf26df, "fanoutList");
            id_b47431f47f8e4e09b40f9e9e914e5594.WireTo(id_703445ea5fb94d7e995fa3811c4640dc, "sourceOutput");
            id_24494b8b4c0d4e07bfc670b28c397c20.WireTo(id_b340bcc2ab47409dbbb6685f3cf072f8, "sourceOutput");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();
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




























































































































































































































