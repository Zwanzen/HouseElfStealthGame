using UnityEngine;

[CreateAssetMenu(fileName = "NpcPath", menuName = "NPC/NpcPath", order = 1)]
public class NpcPath : ScriptableObject
{
    public Waypoint[] Waypoints;
    public bool IsLoop;
}
