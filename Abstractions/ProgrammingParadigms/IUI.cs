using System.Windows;

namespace ProgrammingParadigms
{
    /// <summary>
    /// Hierarchical containment structure of the UI
    /// </summary>
    public interface IUI
    {
        UIElement GetWPFElement();
    }
}
