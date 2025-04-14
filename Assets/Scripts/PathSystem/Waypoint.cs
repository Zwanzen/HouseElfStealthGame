using System;
using UnityEngine;

[Serializable]
public struct Waypoint
{
    public Vector3 Point;
    public bool HasStop;
    public float StopTime;
    public bool HasDirection;
    public Vector3 Direction;
    public bool HasAnimation;
    public AnimationType Animation;
    
    public enum AnimationType
    {
        Idle,
        Think,
        Interact,
        Work,
    }
}
