using UnityEngine;

namespace HardwareEventDesigner.Runtime
{
    public enum HardwareValueType
    {
        Boolean, 
        Float, 
        Integer
    }

    public enum HardwareTriggerMode
    {
        Edge, 
        Hold, 
        Toggle
    }

    [CreateAssetMenu (
        fileName = "NewHardwareEventChannel", 
        menuName = "Hardware/ Hardware Event Channel", 
        order = 0)]

    public class HardwareEventChannel : ScriptableObject
    {
        [Header("Identification")]
        public string channelId = "DoorButton";

        [Header("Value Settings")]
        public HardwareValueType valueType = HardwareValueType.Boolean;

        [Tooltip("Umbral principal para activar el evento (si aplica).")]
        public float threshold = 0.5f;

        [Header("Trigger Mode")]
        public HardwareTriggerMode triggerMode = HardwareTriggerMode.Edge;

        [Header("Debug")]
        [SerializeField, TextArea]
        private string description = "Describe el propósito de este canal.";

        public string Description => description;

        /// <summary>
        /// Decide si un valor debería activar el canal,
        /// según su tipo y configuración.
        /// </summary>
        public bool ShouldActivate(float value)
        {
            switch (valueType)
            {
                case HardwareValueType.Boolean:
                    return value != 0f;

                case HardwareValueType.Float:
                case HardwareValueType.Integer:
                    return value >= threshold;

                default:
                    return false;
            }
        }
    }


}