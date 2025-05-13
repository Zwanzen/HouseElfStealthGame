using UnityEngine;

/// <summary>
/// This class is responsible for managing checkpoints in the game.
/// </summary>
public class CheckpointManager : MonoBehaviour
{ 
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

        if(ActiveCheckpoint != null)
        {
            ActiveCheckpoint.Activate();
        }

        // Set the starting checkpoint
        if (startPoint != null && ActiveCheckpoint == null)
        {
            ActiveCheckpoint = startPoint;
            ActiveCheckpoint.Activate();
        }
        else
        {
            Debug.LogWarning("No starting checkpoint assigned.");
        }
    }

    // Public methods
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
}
