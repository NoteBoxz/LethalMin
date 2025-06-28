using UnityEngine.InputSystem;
using System;

namespace LethalMin
{
    public class InputClass
    {
        private static bool _isUtilsLoaded;
        private static Type _inputClassWithUtilsType = null!;
        private static object _instance = null!;

        static InputClass()
        {
            _isUtilsLoaded = LethalMin.IsDependencyLoaded("com.rune580.LethalCompanyInputUtils");
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
        public InputAction ThrowCancel => GetInputAction("ThrowCancel");
        public InputAction Whistle => GetInputAction("Whistle");
        public InputAction Dismiss => GetInputAction("Dismiss");
        public InputAction SwitchWhistleAud => GetInputAction("SwitchWhistleAud");
        public InputAction SwitchLeft => GetInputAction("SwitchLeft");
        public InputAction SwitchRight => GetInputAction("SwitchRight");
        public InputAction Charge => GetInputAction("Charge");
        public InputAction Glowmob => GetInputAction("Glowmob");
        public InputAction OnionHudSpeed => GetInputAction("OnionHudSpeed");

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