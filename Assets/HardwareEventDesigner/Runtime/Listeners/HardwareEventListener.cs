using UnityEngine;
using UnityEngine.Events;

namespace HardwareEventDesigner.Runtime
{
    public class HardwareEventListener : MonoBehaviour
    {
        [Header("Channel")]
        public HardwareEventChannel channel;

        [Header("Events")]
        public UnityEvent onTriggered;
        public UnityEvent onValueChanged;

        [Header("Value Change Detection")]
        [Tooltip("Cambio mínimo para considerar que el valor realmente cambió (filtra ruido analógico).")]
        [SerializeField] private float changeThreshold = 0.01f;


        private bool isActive;
        private float lastValue;

        public HardwareEventChannel Channel => channel;
        public bool IsActive => isActive;
        public float LastValue => lastValue;
        public void UpdateValue(float value)
        {
            if (channel == null)
            {
                Debug.LogWarning($"HardwareEventListener en {gameObject.name} no tiene canal asignado.");
                return;
            }

            bool wasActive = isActive;
            float previousValue = lastValue;
            
            lastValue = value;
            bool shouldActivate = channel.ShouldActivate(value);

            switch (channel.triggerMode)
            {
                case HardwareTriggerMode.Edge:
                    HandleEdgeMode(shouldActivate, wasActive);
                    break;

                case HardwareTriggerMode.Hold:
                    HandleHoldMode(shouldActivate);
                    break;

                case HardwareTriggerMode.Toggle:
                    HandleToggleMode(shouldActivate, wasActive);
                    break;
            }

            if (Mathf.Abs(previousValue - value) > changeThreshold)
            {
                onValueChanged?.Invoke();
            }
        }

        private void HandleEdgeMode(bool shouldActivate, bool wasActive)
        {
            if (shouldActivate && !wasActive)
            {
                isActive = true;
                onTriggered?.Invoke();
            }
            else if (!shouldActivate && wasActive)
            {
                isActive = false;
            }
        }

        private void HandleHoldMode(bool shouldActivate)
        {
            if (shouldActivate)
            {
                if (!isActive)
                    isActive = true;

                onTriggered?.Invoke();
            }
            else
            {
                if (isActive)
                    isActive = false;
            }
        }

        private void HandleToggleMode(bool shouldActivate, bool wasActive)
        {
            if (shouldActivate && !wasActive)
            {
                isActive = !isActive;
                onTriggered?.Invoke();
            }
        }

        private void OnEnable()
        {
            HardwareInputManager.Register(this);
        }

        private void OnDisable()
        {
            HardwareInputManager.Unregister(this);
        }
    }
}
