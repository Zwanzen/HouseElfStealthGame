using UnityEngine;

/// <summary>
/// Used to mark the area where the player wins.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WinBox : MonoBehaviour
{
    private void Awake()
    {
        // Set the collider to be a trigger
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player has entered the win box
        if (other.CompareTag("Player"))
        {
            // Call the GameManager's Win method
            GameManager.Instance.Win();
        }
    }
}
