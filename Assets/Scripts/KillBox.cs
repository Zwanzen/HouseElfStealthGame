using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillBox : MonoBehaviour
{
    private void Awake()
    {
        // Ensure the collider is set as a trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is a player
        if (other.CompareTag("Player"))
        {
            // Call the GameOver method from the GameManager
            GameManager.Instance.GameOver();
        }
    }
}
