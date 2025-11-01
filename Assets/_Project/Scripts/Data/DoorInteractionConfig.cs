using UnityEngine;

[CreateAssetMenu(fileName = "DoorInteractionConfig", menuName = "Zero Trace/Door Interaction Config")]
public class DoorInteractionConfig : ScriptableObject
{
    [Header("Ranges")]
    public float physicalInteractionRange = 3f;
    public float hackRange = 15f;

    [Header("Prompts")]
    public string physicalUseText = "Open";
    public string hackText = "HACK";
    public string alreadyHackedText = "ACCESS GRANTED";
    public string outOfRangeText = "OUT OF RANGE";
}