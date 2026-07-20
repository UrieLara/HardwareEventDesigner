using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HardwareEventDesigner.Runtime
{
    public class MockHardwareInputProvider : MonoBehaviour, IHardwareInputProvider
    {
        [System.Serializable]
        public class MockChannelValue
        {
            public string channelId;
            [Range(0f, 1f)] public float value;
        }

        [SerializeField] private List<MockChannelValue> channels = new List<MockChannelValue>();

        public float GetValue(string channelId)
        {
            var entry = channels.Find(c => c.channelId == channelId);
            return entry != null ? entry.value : 0f;
        }

        public void SetMockValue(string channelId, float value)
        {
            var entry = channels.Find(c => c.channelId == channelId);
            if (entry != null)
                entry.value = value;
        }
    }
}

