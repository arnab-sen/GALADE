using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries;

namespace ProgrammingParadigms
{
    /// <summary>
    /// Simply propagates an IEventHandler. The main purpose of this is to treat an IEventHandler fanout list as a single-element port, and vice-versa.
    /// </summary>
    public class EventHandlerConnector : IEventHandler
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";

        // Private fields
        private object _lastSender;

        // Ports
        private IEventHandler propagatedHandler;
        private List<IEventHandler> propagatedHandlerFanoutList = new List<IEventHandler>();

        public object Sender
        {
            get => _lastSender;
            set
            {
                _lastSender = value;

                if (propagatedHandler != null) propagatedHandler.Sender = _lastSender;

                foreach (var eventHandler in propagatedHandlerFanoutList)
                {
                    eventHandler.Sender = _lastSender;
                }
            }
        }
        public void Subscribe(string eventName, object sender)
        {
            throw new NotImplementedException();
        }
    }
}
