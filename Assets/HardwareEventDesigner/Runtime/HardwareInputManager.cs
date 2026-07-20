using UnityEngine;

namespace HardwareEventDesigner.Runtime
{
    public class HardwareInputManager : MonoBehaviour
    {
        [Header("Provider")]
        [Tooltip("Arrastra aquí cualquier componente que implemente IHardwareInputProvider.")]
        [SerializeField] private MonoBehaviour _providerBehaviour;

        private IHardwareInputProvider _provider;
        private HardwareEventListener[] _listeners;

        private void Awake()
        {
            _provider = _providerBehaviour as IHardwareInputProvider;

            if (_provider == null)
            {
                Debug.LogError("El provider asignado no implementa IHardwareInputProvider.");
            }

            _listeners = FindObjectsOfType<HardwareEventListener>();
        }

        private void Update()
        {
            if (_provider == null) return;

            foreach (var listener in _listeners)
            {
                var channel = listener.Channel;
                if (channel == null) continue;
                float value = _provider.GetValue(channel.channelId);

                listener.UpdateValue(value);
            }
        }
    }
}
