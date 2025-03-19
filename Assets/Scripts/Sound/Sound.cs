using UnityEngine;

public class Sound
{
    
    public enum ESoundType
    {
        Default = -1,
        Environment,
        Player,
    }

    public Sound(Vector3 pos, float range, float amplitude = 0f)
    {
        Pos = pos;
        Range = range;
        Amplitude = amplitude;
    }

    public ESoundType SoundType;
    
    public readonly Vector3 Pos;
    public readonly float Range;
    public readonly float Amplitude;
}
