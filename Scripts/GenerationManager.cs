using System.Collections.Generic;
using UnityEngine;

namespace LethalMin
{
    public class GenerationManager : MonoBehaviour
    {
        private static GenerationManager _instance = null!;
        public static GenerationManager Instance => _instance;

        private readonly List<IGenerationSwitchable> _switchables = new List<IGenerationSwitchable>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register(IGenerationSwitchable switchable)
        {
            if (!_switchables.Contains(switchable))
            {
                _switchables.Add(switchable);
            }
        }

        public void Unregister(IGenerationSwitchable switchable)
        {
            _switchables.Remove(switchable);
        }

        public void SwitchGeneration(PikminGeneration generation)
        {
            LethalMin.Logger.LogDebug($"Switching to generation: {generation}");

            foreach (var switchable in _switchables)
            {
                switchable.SwitchGeneration(generation);
            }
        }
    }
}