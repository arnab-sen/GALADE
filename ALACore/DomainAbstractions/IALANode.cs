using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;
using Newtonsoft.Json.Linq;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    public delegate void SomethingChangedDelegate();

    public interface IALANode
    {
        string Name { get; }
        string Id { get; set; }
        object NodeModel { get; set; }
        UIElement Render { get; set; }
        event SomethingChangedDelegate PositionChanged;
        void Delete(bool deleteChildren = false);
        string ToInstantiation(bool singleLine = true);
        bool IsReferenceNode { get; set; }

    }
}
