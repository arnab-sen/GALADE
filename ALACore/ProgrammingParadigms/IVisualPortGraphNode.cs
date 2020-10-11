using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    public delegate void NodePositionChangedDelegate();
    public delegate void PortConnectionRequestedDelegate(Port port);

    public interface IVisualPortGraphNode
    {
        string Id { get; set; }
        List<Port> Ports { get; set; }
        VisualPortGraph Graph { get; set; }
        Port SelectedPort { get; set; }
        Dictionary<string, FrameworkElement> PortRenders { get; set; }
        FrameworkElement Render { get; set; }
        double PositionX { get; set; }
        double PositionY { get; set; }
        event NodePositionChangedDelegate PositionChanged;
        string Serialise(HashSet<string> excludeFields = null);
        void Deserialise(string content, HashSet<string> excludeFields = null);
        void Select();
        void Deselect();
        void RefreshUI();
    }
}
