using UnityEngine;

public class Sound
{
    
    public enum ESoundType
    {
        Default = -1,
        Environment,
        Player,
    }

    public Sound(Vector3 pos, float range)
    {
        Pos = pos;
        Range = range;
    }

    public ESoundType SoundType;
    
    public readonly Vector3 Pos;
    public readonly float Range;
}
