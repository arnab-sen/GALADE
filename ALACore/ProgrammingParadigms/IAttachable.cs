using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// Represents an item that can be attached at a point to another IAttachable.
    /// </summary>
    public interface IAttachable
    {
        List<Tuple<IAttachable, Point>> Attachments { get; set; }
        void Attach(IAttachable item, Point point = default);
    }
}
