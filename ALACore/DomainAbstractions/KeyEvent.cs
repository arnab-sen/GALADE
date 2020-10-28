using System;
using System.Windows.Input;
using Libraries;
using ProgrammingParadigms;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Subscribes a KeyEventHandler to an IEventHandler with a user-specified lambda, and propagates the sender, args, and event (as an IEvent) as outputs
    /// if the Condition is true.</para>
    /// <para>The Keys[] array represents the key combination should be considered.</para>
    /// </summary>
    public class KeyEvent : IEventHandler
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public KeyEventHandler Lambda { get; set; }
        public Predicate<KeyEventArgs> Condition { get; set; }
        public Key Key { get; set; } = Key.None;
        public Key[] Keys { get; set; } // The key combination that must be pressed
        public Func<object, object> ExtractSender { get; set; }

        // Private fields
        private string eventToHandle;
        private object _sender;

        // Ports
        private IDataFlow<object> senderOutput;
        private IDataFlow<KeyEventArgs> argsOutput;
        private IEvent eventHappened;

        /// <summary>
        /// <para>Subscribes a KeyEventHandler to an IEventHandler with a user-specified lambda, and propagates the sender, args, and event (as an IEvent) as outputs.</para>
        /// <para>The Lambda definition should follow the following format:<code>Lambda = (sender, args) => { ... }</code></para>
        /// </summary>
        public KeyEvent(string eventName)
        {
            eventToHandle = eventName;
        }

        // IEventHandler<T> implementation
        public object Sender
        {
            get => _sender;
            set
            {
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

                Predicate<KeyEventArgs> keyCondition = null;
                Predicate<Key> keyCheck = null;

                if (eventToHandle == "KeyDown")
                {
                    keyCheck = Keyboard.IsKeyDown;
                }
                else if (eventToHandle == "KeyUp")
                {
                    keyCheck = Keyboard.IsKeyUp;
                }

                if (keyCheck != null)
                {
                    if (Key != Key.None)
                    {
                        keyCondition = args => Keyboard.IsKeyDown(Key);
                    }
                    else if (Keys != null)
                    {
                        keyCondition = args =>
                        {
                            var satisfied = true;

                            foreach (var key in Keys)
                            {
                                if (!Keyboard.IsKeyDown(key))
                                {
                                    satisfied = false;
                                    break;
                                }
                            }

                            return satisfied;
                        };
                    }
                }

                if (keyCondition == null) keyCondition = args => true;

                // Propagate the sender, args, and event (as an IEvent) after the event has been handled by the user-specified lambda
                senderEvent.AddEventHandler(sender, new KeyEventHandler((o, args) =>
                {
                    if (keyCondition(args))
                    {
                        if (Condition?.Invoke(args) ?? true)
                        {
                            if (senderOutput != null) senderOutput.Data = sender;
                            if (argsOutput != null) argsOutput.Data = args;
                            eventHappened?.Execute();
                        } 
                    }
                }));
                
            }
            catch (Exception e)
            {

            }
        }
    }
}
