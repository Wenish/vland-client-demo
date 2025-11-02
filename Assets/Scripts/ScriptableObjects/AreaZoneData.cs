using UnityEngine;

[CreateAssetMenu(fileName = "AreaZone", menuName = "Game/AreaZone/AreaZone")]
public class AreaZoneData : ScriptableObject
{
    public enum TickMode
    {
        EvenlySpacedEndAligned,
        IncludeStartAndEnd
    }
    public string areaZoneName;

    [Tooltip("Duration (in seconds) the area zone remains active.")]
    public float duration;

    [Tooltip("How often the area zone ticks (in seconds). Set to 0 to disable ticking.")]
    [Min(0)]
    public int tickCount;

    [Tooltip("How to schedule ticks during the area zone's duration.")]
    public TickMode tickMode = TickMode.EvenlySpacedEndAligned;
    public GameObject prefabVisual;
}