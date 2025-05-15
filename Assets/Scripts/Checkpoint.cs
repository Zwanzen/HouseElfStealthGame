using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private BoxCollider boxCollider;

    // Constants
    private const string PlayerTag = "Player";
    private const float HeightOffset = 0.55f;

    // Properties
    public bool IsActive { get; private set; } = false;

    /// <summary>
    /// Gets the respawn position of the checkpoint with the correct player height offset.
    /// </summary>
    public Vector3 RespawnPosition
    {
        get
        {
            // Return the respawn position with an offset
            return respawnPoint != null ? respawnPoint.position + Vector3.up * HeightOffset : Vector3.zero;
        }
    }

    /// <summary>
    /// Gets the respawn direction of the checkpoint.
    /// </summary>
    public Vector3 RespawnDirection => respawnPoint != null ? respawnPoint.forward : Vector3.forward;

    private void Awake()
    {
        // Ensure the collider is set as a trigger
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

    }

    // Public methods
    public void Activate()
    {
        // Set the checkpoint as active
        IsActive = true;
        // Turn off collider to prevent multiple activations
        if (boxCollider != null)
            boxCollider.enabled = false;
    }
    public void Deactivate()
    {
        // Set the checkpoint as inactive
        IsActive = false;
    }
    public void Reset()
    {
        // Reset the checkpoint to its initial state
        IsActive = false;
        if (boxCollider != null)
            boxCollider.enabled = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player has entered the checkpoint
        if (other.CompareTag(PlayerTag))
            CheckpointManager.Instance.ActivateCheckpoint(this);
    }

}
