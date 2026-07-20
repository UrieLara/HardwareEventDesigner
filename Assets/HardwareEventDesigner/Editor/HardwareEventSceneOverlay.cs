using UnityEngine;
using UnityEditor;
using HardwareEventDesigner.Runtime;

namespace HardwareEventDesigner.Editor
{
    [InitializeOnLoad]
    public static class HardwareEventSceneOverlay
    {
        static HardwareEventSceneOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, 250, 120), "Hardware Channels", "Window");

            var listeners = Object.FindObjectsOfType<HardwareEventListener>();

            if (listeners.Length == 0)
            {
                GUILayout.Label("No HardwareEventListeners in scene.");
            }
            else
            {
                foreach (var listener in listeners)
                {
                    var channel = listener.Channel;
                    if (channel == null) continue;

                    GUILayout.Label($"{channel.channelId} | Value: {listener.LastValue:F2} | Active: {listener.IsActive}");
                }
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}

