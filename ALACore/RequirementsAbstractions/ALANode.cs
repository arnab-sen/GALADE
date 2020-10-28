using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public UIElement Render { get; set; }
        public AbstractionModel Model { get; set; }

        // Private fields
        private Box rootUI;

        // Ports

        // Methods
        private void SetWiring()
        {
            rootUI = new Box();
            Model = CreateDummyAbstractionModel();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Horizontal id_eb9ca53eea6e451c97c660866e132b84 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle" };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_0a7f12d4c76444a49750d4f53a92b21a = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_70a29676ee6a42a1a9d4a669199828a6 = new ConvertToEvent<object>() {  };
            EventConnector id_85e20fca9efa43fdac5f66e6753b303c = new EventConnector() {  };
            Data<object> id_be66cff8314c4e638896a5ad55a376c5 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_2399ef6d8d824b839f9b392ffe8434a0 = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_9f25c2e283434922af2449d43993b997 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_256f0fc11daf4192ac96d0412422ad6c = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text, "uiLayout");inputPortsVertConnector.Data.WireTo(box, "children");return box;} };
            UIFactory id_6078da7c78be496d870903602ef48c19 = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();return outputPortsVert ;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_9f2f97b59bc14f859a971c2c9a9bb333 = new ConvertToEvent<object>() {  };
            EventConnector id_9fad9f32a7ee47c5b4e6456271435a3a = new EventConnector() {  };
            Data<object> id_bf5e809758814a4cbcbccc70a775ebed = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_3c82ab46a0104005999046bc90c4103d = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_44f7feaf89ce467086c3202393f25478 = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_4aeae636fc0f47368e3bb9d1706c5741 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            ApplyAction<object> id_8c5c78b5f9154f8d963396d2f6baa962 = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderThickness = new Thickness(3);box.BorderColour = Brushes.Red;} };
            DataFlowConnector<object> id_411cade9ded34fdfa6f1da4414a9983f = new DataFlowConnector<object>() {  };
            ApplyAction<object> id_34e5a417d2fd4c1f944499be76ff185a = new ApplyAction<object>() { Lambda = input =>{var box = input as Box;box.BorderThickness = new Thickness(1);box.BorderColour = Brushes.Black;} };
            ApplyAction<object> id_bd6d33a71b6d4fdeaf829bda18cad9b6 = new ApplyAction<object>() { Lambda = input =>{(input as Box).InitialiseUI();} };
            Apply<object, object> id_c1b1f99afc814877b20e4197f5913478 = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseEnter");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_bbb800e265674c4abc0deebf088cbff6 = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            Apply<object, object> id_94b03fde19214b31a7adaaf51874851f = new Apply<object, object>() { Lambda = input =>{var mouseEvent = new MouseEvent("MouseLeave");mouseEvent.ExtractSender = source => (source as Box).Render;input.WireTo(mouseEvent, "eventHandlers");return mouseEvent;} };
            DynamicWiring<IDataFlow<object>> id_20b59e70ce434aeebece2d1b759d54fa = new DynamicWiring<IDataFlow<object>>() { SourcePort = "sourceOutput" };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_eb9ca53eea6e451c97c660866e132b84, "uiLayout");
            id_eb9ca53eea6e451c97c660866e132b84.WireTo(id_0a7f12d4c76444a49750d4f53a92b21a, "children");
            id_eb9ca53eea6e451c97c660866e132b84.WireTo(nodeMiddle, "children");
            id_eb9ca53eea6e451c97c660866e132b84.WireTo(id_6078da7c78be496d870903602ef48c19, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_0a7f12d4c76444a49750d4f53a92b21a.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_70a29676ee6a42a1a9d4a669199828a6, "fanoutList");
            id_70a29676ee6a42a1a9d4a669199828a6.WireTo(id_85e20fca9efa43fdac5f66e6753b303c, "eventOutput");
            id_85e20fca9efa43fdac5f66e6753b303c.WireTo(id_be66cff8314c4e638896a5ad55a376c5, "fanoutList");
            id_be66cff8314c4e638896a5ad55a376c5.WireTo(id_2399ef6d8d824b839f9b392ffe8434a0, "dataOutput");
            id_2399ef6d8d824b839f9b392ffe8434a0.WireTo(id_9f25c2e283434922af2449d43993b997, "output");
            id_9f25c2e283434922af2449d43993b997.WireTo(id_256f0fc11daf4192ac96d0412422ad6c, "elementOutput");
            id_256f0fc11daf4192ac96d0412422ad6c.WireTo(setUpInputPortBox, "output");
            setUpInputPortBox.WireTo(id_411cade9ded34fdfa6f1da4414a9983f, "output");
            id_6078da7c78be496d870903602ef48c19.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_9f2f97b59bc14f859a971c2c9a9bb333, "fanoutList");
            id_9f2f97b59bc14f859a971c2c9a9bb333.WireTo(id_9fad9f32a7ee47c5b4e6456271435a3a, "eventOutput");
            id_9fad9f32a7ee47c5b4e6456271435a3a.WireTo(id_bf5e809758814a4cbcbccc70a775ebed, "fanoutList");
            id_bf5e809758814a4cbcbccc70a775ebed.WireTo(id_3c82ab46a0104005999046bc90c4103d, "dataOutput");
            id_3c82ab46a0104005999046bc90c4103d.WireTo(id_44f7feaf89ce467086c3202393f25478, "output");
            id_44f7feaf89ce467086c3202393f25478.WireTo(id_4aeae636fc0f47368e3bb9d1706c5741, "elementOutput");
            id_4aeae636fc0f47368e3bb9d1706c5741.WireTo(setUpOutputPortBox, "output");
            id_bbb800e265674c4abc0deebf088cbff6.WireTo(id_8c5c78b5f9154f8d963396d2f6baa962, "wire");
            id_411cade9ded34fdfa6f1da4414a9983f.WireTo(id_c1b1f99afc814877b20e4197f5913478, "fanoutList");
            id_411cade9ded34fdfa6f1da4414a9983f.WireTo(id_94b03fde19214b31a7adaaf51874851f, "fanoutList");
            id_411cade9ded34fdfa6f1da4414a9983f.WireTo(id_bd6d33a71b6d4fdeaf829bda18cad9b6, "fanoutList");
            id_20b59e70ce434aeebece2d1b759d54fa.WireTo(id_34e5a417d2fd4c1f944499be76ff185a, "wire");
            id_c1b1f99afc814877b20e4197f5913478.WireTo(id_bbb800e265674c4abc0deebf088cbff6, "output");
            id_94b03fde19214b31a7adaaf51874851f.WireTo(id_20b59e70ce434aeebece2d1b759d54fa, "output");
            // END AUTO-GENERATED WIRING

            Render = (rootUI as IUI).GetWPFElement();
        }

        private AbstractionModel CreateDummyAbstractionModel()
        {
            var model = new AbstractionModel();
            model.AddImplementedPort("IEvent", "input1");
            model.AddImplementedPort("IEvent", "input2");
            model.AddImplementedPort("IEvent", "input3");
            model.AddAcceptedPort("IEvent", "complete");
            model.AddProperty("Type", "Box");
            model.AddProperty("Name", "test");

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


















































































