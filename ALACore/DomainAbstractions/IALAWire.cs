using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace DomainAbstractions
{
    public interface IALAWire
    {
        string Id { get; set; }
        object SourceNode { get; set; }
        object DestinationNode { get; set; }
        void SetSourceNodeAndPort(object source, object port);
        void SetDestinationNodeAndPort(object destination, object port);
        UIElement Render { get; set; }
        int DefaultZIndex { get; set; }
        bool IsHighlighted { get; set; }
        string ToWireTo(JObject metaData = null);
    }
}
