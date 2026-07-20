using UnityEditor;
using UnityEngine;
using HardwareEventDesigner.Runtime;

namespace HardwareEventDesigner.Editor
{
    public class HardwareEventDesignerWindow : EditorWindow
    {
        private HardwareEventChannel selectedChannel;
        private float simulatedValue;

        [MenuItem("Tools/Hardware Event Designer")]
        public static void OpenWindow()
        {
            var window = GetWindow<HardwareEventDesignerWindow>("Hardware Event Designer");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Hardware Event Designer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawChannelSelection();
            EditorGUILayout.Space();

            if (selectedChannel != null)
            {
                DrawChannelDetails();
                EditorGUILayout.Space();
                DrawSimulationControls();
            }
        }

        private void DrawChannelSelection()
        {
            EditorGUILayout.LabelField("Channel", EditorStyles.boldLabel);

            selectedChannel = (HardwareEventChannel)EditorGUILayout.ObjectField(
                "Selected Channel",
                selectedChannel,
                typeof(HardwareEventChannel),
                false);

            if (GUILayout.Button("Create New Channel"))
            {
                CreateNewChannel();
            }
        }

        private void CreateNewChannel()
        {
            var channel = ScriptableObject.CreateInstance<HardwareEventChannel>();
            channel.channelId = "NewChannel";

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Hardware Event Channel",
                "NewHardwareEventChannel",
                "asset",
                "Choose a location to save the channel asset."
                );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(channel, path);
                AssetDatabase.SaveAssets();
                selectedChannel = channel;
                EditorGUIUtility.PingObject(channel);
            }

        }

        private void DrawChannelDetails()
        {
            EditorGUILayout.LabelField("Channel Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            selectedChannel.channelId = EditorGUILayout.TextField("Channel ID", selectedChannel.channelId);
            selectedChannel.valueType = (HardwareValueType)EditorGUILayout.EnumPopup("Value Type", selectedChannel.valueType);
            selectedChannel.triggerMode = (HardwareTriggerMode)EditorGUILayout.EnumPopup("Trigger Mode", selectedChannel.triggerMode);
            selectedChannel.threshold = EditorGUILayout.Slider("Threshold", selectedChannel.threshold, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(selectedChannel);
            }

        }

        private void DrawSimulationControls()
        {
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);

            simulatedValue = EditorGUILayout.Slider("Simulated Value", simulatedValue, 0f, 1f);

            if(GUILayout.Button("Apply Simulated Value to Scene"))
            {
                ApplySimulatedValueToListeners();
            }
        }

        private void ApplySimulatedValueToListeners()
        {
            var provider = FindObjectOfType<MockHardwareInputProvider>();

            if (provider != null)
            {
                provider.SetMockValue(selectedChannel.channelId, simulatedValue);
                Debug.Log($"Simulated value {simulatedValue} applied to provider for channel {selectedChannel.channelId}.");
            }
            else
            {
                Debug.LogWarning("No MockHardwareInputProvider found in scene.");
            }

        }
    }
}
