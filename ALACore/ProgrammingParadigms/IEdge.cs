using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgrammingParadigms
{
    public interface IEdge
    {
        object Source { get; set; }
        object Destination { get; set; }
        Dictionary<string, object> Payload { get; set; }
    }
}
