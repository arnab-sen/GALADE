using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using ProgrammingParadigms;
using DomainAbstractions;

namespace Application
{
    public class PortGraphNodeUI : IUI
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields

        // Ports

        // IUI implementation
        UIElement IUI.GetWPFElement()
        {
            Horizontal rootUI = new Horizontal();

            // BEGIN AUTO-GENERATED INSTANTIATIONS
            Vertical id_74ef55cf9bbf44ecbef5401b28ade1f9 = new Vertical() {  };
            Vertical id_a9c1c5cd02a942a880888b7cf8a9a741 = new Vertical() {  };
            Vertical id_d9bbf46cd2ce4512b29056fd195fba4c = new Vertical() {  };
            Box id_1d74f36887ca433cad895e5cf12011c1 = new Box() { Width = 20, Height = 10 };
            Box id_145a6622ad684d6fa951b4d9b530be73 = new Box() { Width = 20, Height = 10 };
            TextBox id_5d4ecb9b3e134cf98582305522be8385 = new TextBox() { Width = 100 };
            // END AUTO-GENERATED INSTANTIATIONS

            // BEGIN AUTO-GENERATED WIRING
            rootUI.WireTo(id_74ef55cf9bbf44ecbef5401b28ade1f9, "children");
            rootUI.WireTo(id_a9c1c5cd02a942a880888b7cf8a9a741, "children");
            rootUI.WireTo(id_d9bbf46cd2ce4512b29056fd195fba4c, "children");
            id_74ef55cf9bbf44ecbef5401b28ade1f9.WireTo(id_1d74f36887ca433cad895e5cf12011c1, "children");
            id_a9c1c5cd02a942a880888b7cf8a9a741.WireTo(id_5d4ecb9b3e134cf98582305522be8385, "children");
            id_d9bbf46cd2ce4512b29056fd195fba4c.WireTo(id_145a6622ad684d6fa951b4d9b530be73, "children");
            // END AUTO-GENERATED WIRING

            return (rootUI as IUI).GetWPFElement();
        }

        // Methods

        public PortGraphNodeUI()
        {
            
        }
    }
}














