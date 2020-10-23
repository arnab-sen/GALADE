using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// Represents an item that can be attached at a point to another IAttachable.
    /// When two items are attached, movement of a source should try to move its children.
    /// </summary>
    public interface IAttachable
    {
        Canvas Canvas { get; set; }
        List<IAttachable> AttachedItems { get; }
        void Attach(IAttachable candidate); // Let the source decide the position
        void Attach(IAttachable candidate, Point location); // Let the candidate suggest a position
        Point FindNearestAttachmentPoint(Point candidatePoint);
        void Displace(double xDelta, double yDelta);
        void MoveTo(Point point);
    }
}
