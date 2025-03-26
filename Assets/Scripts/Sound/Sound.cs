using System;
using UnityEngine;

[Serializable]
public class Sound
{
    
    public enum ESoundType
    {
        Default = -1,
        Environment,
        Player,
    }

    public Sound(Vector3 pos, float range, float amplitude = 0f, bool loop = false)
    {
        Pos = pos;
        Range = range;
        Amplitude = amplitude;
        Loop = loop;
    }
    
    public void UpdateSoundPosition(Vector3 newPos)
    {
        Pos = newPos;
    }

    public ESoundType SoundType;
    
    public Vector3 Pos;
    public readonly float Range;
    public readonly float Amplitude;
    public readonly bool Loop;
    
    public float CurrentVolume;
}
