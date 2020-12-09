using System;
using System.Windows;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Subscribes a RoutedEventHandler to an IEventHandler with a user-specified lambda, and propagates the sender, args, and event (as an IEvent) as outputs
    /// if the Condition is true.</para>
    /// <para>The Lambda definition should follow the following format:<code>Lambda = (sender, args) => { ... }</code></para>
    /// </summary>
    public class RoutedEventSubscriber : IEventHandler // sender
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public RoutedEventHandler Lambda { get; set; }
        public Predicate<RoutedEventArgs> Condition { get; set; }
        public Func<object, object> ExtractSender { get; set; }

        // Private fields
        private string eventToHandle;
        private object _source;
        private object _sender;

        // Ports
        private IDataFlow<object> sourceOutput;
        private IDataFlow<object> senderOutput;
        private IDataFlow<RoutedEventArgs> argsOutput;
        private IEvent eventHappened;

        /// <summary>
        /// <para>Subscribes a RoutedEventHandler to an IEventHandler with a user-specified lambda, and propagates the sender, args, and event (as an IEvent) as outputs.</para>
        /// <para>The Lambda definition should follow the following format:<code>Lambda = (sender, args) => { ... }</code></para>
        /// </summary>
        public RoutedEventSubscriber(string eventName)
        {
            eventToHandle = eventName;
        }

        // IEventHandler<T> implementation
        public object Sender
        {
            get => _sender;
            set
            {
                _source = value;
                _sender = ExtractSender != null ? ExtractSender(value) : value;

                Subscribe(eventToHandle, _sender);
            }
        }

        public void Subscribe(string eventName, object sender)
        {
            var senderType = sender.GetType();
            var senderEvent = senderType.GetEvent(eventName);

            try
            {
                // Subscribe the user-specified lambda
                if (Lambda != null) senderEvent.AddEventHandler(sender, Lambda);

                // Propagate the sender, args, and event (as an IEvent) after the event has been handled by the user-specified lambda
                senderEvent.AddEventHandler(sender, new RoutedEventHandler((o, args) =>
                {
                    if (Condition?.Invoke(args) ?? true)
                    {
                        if (sourceOutput != null) sourceOutput.Data = _source;
                        if (senderOutput != null) senderOutput.Data = sender;
                        if (argsOutput != null) argsOutput.Data = args;
                        eventHappened?.Execute(); 
                    }
                }));
                
            }
            catch (Exception e)
            {

            }
        }
    }
}
