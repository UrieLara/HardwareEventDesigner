using UnityEngine;

public class DebugReceiver : MonoBehaviour
{
    public void PrintTriggered()
    {
        Debug.Log("TRIGGERED!");
    }

    public void PrintValueChanged()
    {
        Debug.Log("VALUE CHANGED!");
    }
}
