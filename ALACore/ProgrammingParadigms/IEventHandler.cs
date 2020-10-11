using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    public interface IEventHandler
    {
        object Sender { get; set; }
        void Subscribe(string eventName, object sender);
    }
}
