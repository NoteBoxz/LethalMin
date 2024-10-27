using UnityEngine.InputSystem;
using System;

namespace LethalMin
{
    public class InputClass
    {
        private static bool _isUtilsLoaded;
        private static Type _inputClassWithUtilsType;
        private static object _instance;

        static InputClass()
        {
            _isUtilsLoaded = LethalMin.IsUsingInputUtils();
            if (_isUtilsLoaded)
            {
                _inputClassWithUtilsType = Type.GetType("InputClassWithUtils");
                if (_inputClassWithUtilsType != null)
                {
                    _instance = Activator.CreateInstance(_inputClassWithUtilsType);
                }
            }
        }

        public InputAction Throw => GetInputAction("Throw");
        public InputAction Whistle => GetInputAction("Whistle");
        public InputAction Dismiss => GetInputAction("Dismiss");
        public InputAction SwitchLeft => GetInputAction("SwitchLeft");
        public InputAction SwitchRight => GetInputAction("SwitchRight");

        private InputAction GetInputAction(string name)
        {
            if (_isUtilsLoaded && _instance != null)
            {
                return (InputAction)_inputClassWithUtilsType.GetProperty(name).GetValue(_instance);
            }
            return new InputAction(name);
        }
    }
}