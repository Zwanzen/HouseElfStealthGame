using UnityEngine;
using System;

/// <summary>
/// This class is used to broadcast collision events.
/// Any class that needs to listen for collisions from this object can subscribe to the events.
/// </summary>
public class CollisionBroadcaster : MonoBehaviour
{
    // Events
    public event Action<Collision> OnCollisionEnterEvent;
    public event Action<Collision> OnCollisionStayEvent;
    public event Action<Collision> OnCollisionExitEvent;


    private void OnCollisionEnter(Collision collision)
    {
        // Invoke the event if there are any subscribers
        OnCollisionEnterEvent?.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Invoke the event if there are any subscribers
        OnCollisionStayEvent?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        // Invoke the event if there are any subscribers
        OnCollisionExitEvent?.Invoke(collision);
    }
}