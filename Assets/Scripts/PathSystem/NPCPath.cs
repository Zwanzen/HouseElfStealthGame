using UnityEngine;

[CreateAssetMenu(fileName = "NpcPath", menuName = "NPC/NpcPath", order = 1)]
public class NPCPath : ScriptableObject
{
    public Waypoint[] Waypoints;
    public Vector3[] Positions;
    public bool IsLoop;
}
