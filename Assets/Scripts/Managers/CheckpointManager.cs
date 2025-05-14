using UnityEngine;

/// <summary>
/// This class is responsible for managing checkpoints in the game.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] private Checkpoint startPoint;

    // Properties
    public Checkpoint ActiveCheckpoint { get; private set; }

    // Singleton instance
    public static CheckpointManager Instance { get; private set; }
    private void Awake()
    {
        // Ensure that there is only one instance of CheckpointManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ___ Private methods ___
    private void SetFirstCheckpoint()
    {
        // Reset all checkpoints
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.Reset();
        }

        // Set the first checkpoint as the active checkpoint
        if (startPoint != null)
        {
            ActiveCheckpoint = startPoint;
            ActiveCheckpoint.Activate();
        }
        else
        {
            ActiveCheckpoint = checkpoints[0];
            ActiveCheckpoint.Activate();
        }
    }

    // ___ Public methods ___
    /// <summary>
    /// Called when the game starts, or initilizes.
    /// </summary>
    public void OnGameInitialize()
    {
        SetFirstCheckpoint();
    }

    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        // Deactivate the previous checkpoint if it exists
        if (ActiveCheckpoint != null)
        {
            // Make sure the previous checkpoint is destroyed on load
            ActiveCheckpoint.Deactivate();
        }
        // Activate the new checkpoint
        ActiveCheckpoint = checkpoint;
        ActiveCheckpoint.Activate();

    }

    public void TeleportToLastCheckpoint()
    {
        if(ActiveCheckpoint == null)
        {
            Debug.LogError("No active checkpoint to teleport to.");
            return;
        }
        Debug.Log($"Teleporting to checkpoint: {ActiveCheckpoint}");
        PlayerController.Instance.Teleport(ActiveCheckpoint.RespawnPosition, ActiveCheckpoint.RespawnDirection);
    }
}
