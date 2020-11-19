using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using WindowsInput.Native;
using Libraries;
using ProgrammingParadigms;
using WindowsInput;

namespace DomainAbstractions
{
    /// <summary>
    /// <para>Simulates a key press on the keyboard. The desired key can be set through the string Key property.</para>
    /// <para>Common mappings:</para>
    /// <code>A-Z and 0-9 = A-Z and 0-9</code>
    /// <code> Enter = ACCEPT</code>
    /// <code> Backspace = BACK</code>
    /// <code> Delete = DELETE</code>
    /// <code> Tab = TAB</code>
    /// <code> Either shift = SHIFT</code>
    /// <code> Left shift/right shift = LSHIFT/RSHIFT</code>
    /// </summary>
    class SimulateKeyboard : IEvent
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public string Key { get; set; } = "ACCEPT";

        // Private fields
        private InputSimulator _inputSim = new InputSimulator();

        // Ports

        // IEvent implementation
        void IEvent.Execute()
        {
            SimulateKey(Key);
        }

        // Methods
        public VirtualKeyCode StringToVirtualKeyCode(string key)
        {
            bool success = Enum.TryParse(key, out VirtualKeyCode keyCode);

            if (!success)
            {
                Enum.TryParse("VK_" + key, out keyCode);
            }

            if (!success)
            {
                Logging.Log($"Failed to convert key \"{key}\" to WindowsInput.Native.VirtualKeyCode");
            }

            return keyCode;
        }

        public void SimulateKey(string key)
        {
            try
            {
                key = key.ToUpper();
                var keyCode = StringToVirtualKeyCode(key);

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    _inputSim.Keyboard.KeyDown(keyCode);
                }, DispatcherPriority.ApplicationIdle);
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to simulate key press for key {key} in KeyboardSimulatory.\nException: {e}");
            }
        }

        public SimulateKeyboard()
        {

        }
    }
}
