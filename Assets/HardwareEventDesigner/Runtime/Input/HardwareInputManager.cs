using System.Collections.Generic;
using UnityEngine;

namespace HardwareEventDesigner.Runtime
{
    public class HardwareInputManager : MonoBehaviour
    {
        [Header("Provider")]
        [Tooltip("Arrastra aquí cualquier componente que implemente IHardwareInputProvider.")]
        [SerializeField] private MonoBehaviour _providerBehaviour;

        private IHardwareInputProvider _provider;
        private static readonly List<HardwareEventListener> _listeners = new List<HardwareEventListener>();

        public static void Register(HardwareEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public static void Unregister(HardwareEventListener listener)
        {
            _listeners.Remove(listener);
        }

        private void Awake()
        {
            _provider = _providerBehaviour as IHardwareInputProvider;

            if (_provider == null)
            {
                Debug.LogError("El provider asignado no implementa IHardwareInputProvider.");
                enabled = false;
            }

        }

        private void Update()
        {
            for (int i = 0; i < _listeners.Count; i++)
            {
                var listener = _listeners[i];
                if (listener == null) continue; // objeto destruido entre frames

                var channel = listener.Channel;
                if (channel == null) continue;

                listener.UpdateValue(_provider.GetValue(channel.channelId));
            }
        }
    }
}
