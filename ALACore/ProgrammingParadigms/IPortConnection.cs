using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Libraries;

namespace ProgrammingParadigms
{
    public interface IPortConnection
    {
        string Id { get; set; }
        VisualPortGraph Graph { get; set; }
        string SourceId { get; set; }
        string DestinationId { get; set; }
        Port SourcePort { get; set; }
        Port DestinationPort { get; set; }
        Point SourcePosition { get; set; }
        Point DestinationPosition { get; set; }
        Path Render { get; set; }
        void Select();
        void Deselect();
        void ChangeSource(string nodeId, Port sourcePort);
        void ChangeDestination(string nodeId, Port destinationPort);
        void Validate();
        string Serialise();
        void Deserialise(string memento);
    }
}