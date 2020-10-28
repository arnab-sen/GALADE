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
            Horizontal id_43cd1acdb642456fa74193558d010752 = new Horizontal() {  };
            Vertical nodeMiddle = new Vertical() { InstanceName = "nodeMiddle" };
            Horizontal nodeIdRow = new Horizontal() { InstanceName = "nodeIdRow" };
            DropDownMenu nodeTypeDropDownMenu = new DropDownMenu() { InstanceName = "nodeTypeDropDownMenu", Text = Model.GetValue("Type"), Width = 100 };
            TextBox nodeNameTextBox = new TextBox() { InstanceName = "nodeNameTextBox", Text = Model.GetValue("Name"), Width = 50 };
            UIFactory id_7e8aa0e253b746d097c7bc88fb7d14ed = new UIFactory(getUIContainer: () =>{var inputPortsVert = new Vertical();return inputPortsVert;}) {  };
            DataFlowConnector<object> inputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "inputPortsVertConnector" };
            ConvertToEvent<object> id_7226ed6945914103a1af935f606716ed = new ConvertToEvent<object>() {  };
            EventConnector id_51aaf0390c26480e99fcd8beaa33267d = new EventConnector() {  };
            Data<object> id_0c3e34f828514d28bc15a5212651ca95 = new Data<object>() { Lambda = Model.GetImplementedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_4ba10f514b22445ca2029fcaa8103ddb = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_439a308bc1da44fe8d2ed9281a74851a = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_e73ff988be664821ad4c26d791ccfa3d = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = true;return port;} };
            Apply<object, object> setUpInputPortBox = new Apply<object, object>() { InstanceName = "setUpInputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);inputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            UIFactory id_3562cd0a51a545169880db2301fec8cf = new UIFactory(getUIContainer: () =>{var outputPortsVert = new Vertical();return outputPortsVert ;}) {  };
            DataFlowConnector<object> outputPortsVertConnector = new DataFlowConnector<object>() { InstanceName = "outputPortsVertConnector" };
            ConvertToEvent<object> id_6d3fd51f68b547958294407382f3b484 = new ConvertToEvent<object>() {  };
            EventConnector id_7a0214816ff1477d8ae92f632ddc5697 = new EventConnector() {  };
            Data<object> id_a399341c29b244168d9d7dee35291560 = new Data<object>() { Lambda = Model.GetAcceptedPorts };
            Cast<object, IEnumerable<KeyValuePair<string, string>>> id_f24b4b3936da4d4f918485822098073d = new Cast<object, IEnumerable<KeyValuePair<string, string>>>() {  };
            ForEach<KeyValuePair<string, string>> id_7bb8f4bf3a904b8f90af45d168097a0d = new ForEach<KeyValuePair<string, string>>() {  };
            Apply<KeyValuePair<string, string>, object> id_bd89a7ce2b6a4a948fa57f551bd440c6 = new Apply<KeyValuePair<string, string>, object>() { Lambda = input =>{var port = new Port();port.Type = input.Value;port.Name = input.Key;port.IsInputPort = false;return port;} };
            Apply<object, object> setUpOutputPortBox = new Apply<object, object>() { InstanceName = "setUpOutputPortBox", Lambda = input =>{var box = new Box();box.Payload = input;box.Width = 50;box.Height = 15;box.Background = Brushes.White;var toolTipLabel = new System.Windows.Controls.Label() { Content = (input as Port).FullName };box.Render.ToolTip = new System.Windows.Controls.ToolTip() { Content = toolTipLabel };box.Render.MouseEnter += (sender, args) => toolTipLabel.Content = (input as Port).ToString();var text = new Text(text: (input as Port).Name);text.HorizAlignment = HorizontalAlignment.Center;box.WireTo(text);outputPortsVertConnector.Data.WireTo(box, "children");box.InitialiseUI();return box;} };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_43cd1acdb642456fa74193558d010752, "uiLayout");
            id_43cd1acdb642456fa74193558d010752.WireTo(id_7e8aa0e253b746d097c7bc88fb7d14ed, "children");
            id_43cd1acdb642456fa74193558d010752.WireTo(nodeMiddle, "children");
            id_43cd1acdb642456fa74193558d010752.WireTo(id_3562cd0a51a545169880db2301fec8cf, "children");
            nodeMiddle.WireTo(nodeIdRow, "children");
            nodeIdRow.WireTo(nodeTypeDropDownMenu, "children");
            nodeIdRow.WireTo(nodeNameTextBox, "children");
            id_7e8aa0e253b746d097c7bc88fb7d14ed.WireTo(inputPortsVertConnector, "uiInstanceOutput");
            inputPortsVertConnector.WireTo(id_7226ed6945914103a1af935f606716ed, "fanoutList");
            id_7226ed6945914103a1af935f606716ed.WireTo(id_51aaf0390c26480e99fcd8beaa33267d, "eventOutput");
            id_51aaf0390c26480e99fcd8beaa33267d.WireTo(id_0c3e34f828514d28bc15a5212651ca95, "fanoutList");
            id_0c3e34f828514d28bc15a5212651ca95.WireTo(id_4ba10f514b22445ca2029fcaa8103ddb, "dataOutput");
            id_4ba10f514b22445ca2029fcaa8103ddb.WireTo(id_439a308bc1da44fe8d2ed9281a74851a, "output");
            id_439a308bc1da44fe8d2ed9281a74851a.WireTo(id_e73ff988be664821ad4c26d791ccfa3d, "elementOutput");
            id_e73ff988be664821ad4c26d791ccfa3d.WireTo(setUpInputPortBox, "output");
            id_3562cd0a51a545169880db2301fec8cf.WireTo(outputPortsVertConnector, "uiInstanceOutput");
            outputPortsVertConnector.WireTo(id_6d3fd51f68b547958294407382f3b484, "fanoutList");
            id_6d3fd51f68b547958294407382f3b484.WireTo(id_7a0214816ff1477d8ae92f632ddc5697, "eventOutput");
            id_7a0214816ff1477d8ae92f632ddc5697.WireTo(id_a399341c29b244168d9d7dee35291560, "fanoutList");
            id_a399341c29b244168d9d7dee35291560.WireTo(id_f24b4b3936da4d4f918485822098073d, "dataOutput");
            id_f24b4b3936da4d4f918485822098073d.WireTo(id_7bb8f4bf3a904b8f90af45d168097a0d, "output");
            id_7bb8f4bf3a904b8f90af45d168097a0d.WireTo(id_bd89a7ce2b6a4a948fa57f551bd440c6, "elementOutput");
            id_bd89a7ce2b6a4a948fa57f551bd440c6.WireTo(setUpOutputPortBox, "output");
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
































































