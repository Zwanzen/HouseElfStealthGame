using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is responsible for managing the game state,
/// loading scenes, menues, and so on.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        
        // Temp mouse lock
        Cursor.lockState = CursorLockMode.Locked;

        // Teleport to the current checkpoint
        var currentCheckpoint = CheckpointManager.Instance.ActiveCheckpoint;
        PlayerController.Instance.Teleport(currentCheckpoint.RespawnPosition, currentCheckpoint.RespawnDirection);
    }


    private void Update()
    {
        // Temp
        if(Input.GetKeyDown(KeyCode.O))
        {
            // Unlock the cursor
            OnLoose();
        }
    }

    // Private methods

    private void OnLevelWasLoaded(int level)
    {
        /*
        var currentCheckpoint = CheckpointManager.Instance.ActiveCheckpoint;
        PlayerController.Instance.Teleport(currentCheckpoint.RespawnPosition, currentCheckpoint.RespawnDirection);
        */
    }

    // Public methods
    public void OnLoose()
    {
        // We want to load the current scene again
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
