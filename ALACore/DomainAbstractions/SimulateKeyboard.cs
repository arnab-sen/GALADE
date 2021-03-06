﻿using System;
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
    /// <para>Simulates key presses on the keyboard. The desired keys can be set through the Keys property, and any modifiers through the Modifiers property.</para>
    /// <para>Requires the InputSimulator NuGet package: <code>https://www.nuget.org/packages/InputSimulator/</code> <code>https://github.com/michaelnoonan/inputsimulator</code></para>
    /// <para>Common mappings:</para>
    /// <code>A-Z and 0-9 = A-Z and 0-9</code>
    /// <code>Enter = ENTER</code>
    /// <code>Backspace = BACK</code>
    /// <code>Delete = DELETE</code>
    /// <code>Tab = TAB</code>
    /// <code>Either shift = SHIFT, left/right shift = LSHIFT/RSHIFT</code>
    /// <code>Either ctrl = CONTROL, left/right ctrl = LCONTROL/RCONTROL</code>
    /// <code>Either alt = ALT, left/right alt = LALT/RALT</code>
    /// </summary>
    public class SimulateKeyboard : IEvent // start
    {
        // Public fields and properties
        public string InstanceName { get; set; } = "Default";
        public List<string> Keys { get; set; } = new List<string>() {  };
        public List<string> Modifiers { get; set; } = new List<string>() {  };

        // Private fields
        private InputSimulator _inputSim = new InputSimulator();
        private Dictionary<string, string> _readableKeyMapping = new Dictionary<string, string>();

        // Ports

        // IEvent implementation
        void IEvent.Execute()
        {
            SimulateKeys(Modifiers, Keys);
        }

        // Methods
        public VirtualKeyCode StringToVirtualKeyCode(string key)
        {
            if (_readableKeyMapping.ContainsKey(key)) key = _readableKeyMapping[key];

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

        /// <summary>
        /// Simulate a single key press.
        /// </summary>
        /// <param name="key"></param>
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

        /// <summary>
        /// Simulate multiple keys, including modifiers. Modifiers will be held down in order, then the regular keys will be pressed in order.
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="keys"></param>
        public void SimulateKeys(List<string> modifiers, List<string> keys)
        {
            try
            {
                var upperMods = modifiers.Select(m => m.ToUpper());
                var upperKeys = keys.Select(k => k.ToUpper());

                var modCodes = upperMods.Select(StringToVirtualKeyCode);
                var keyCodes = upperKeys.Select(StringToVirtualKeyCode);

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    _inputSim.Keyboard.ModifiedKeyStroke(modCodes, keyCodes);

                }, DispatcherPriority.ApplicationIdle);
            }
            catch (Exception e)
            {
                Logging.Log($"Failed to simulate key press for modifiers {Modifiers} and keys {Keys} in KeyboardSimulator.\nException: {e}");
            }
        }

        public SimulateKeyboard()
        {
            _readableKeyMapping["ENTER"] = "RETURN";
            _readableKeyMapping["ALT"] = "MENU";
            _readableKeyMapping["LALT"] = "LMENU";
            _readableKeyMapping["RALT"] = "RMENU";
            _readableKeyMapping["COMMA"] = "OEM_COMMA";
        }
    }
}
