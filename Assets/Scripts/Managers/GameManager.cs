using FMODUnity;
using System;
using System.Collections;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class is responsible for managing the game state,
/// loading scenes, menues, and so on.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private Image fadeImage; // The image that fades the screen to black when reloading the scene

    // Singleton instance
    public static GameManager Instance;

    // ___ Private variables ___
    private bool isGameStarted = false; // only happens once
    private float fadeTimer = 1f; // Timer for the fade effect
    private float fadeDuration = 0.5f; // Duration of the fade effect
    private float blackScreenDelay = 0.5f; // Delay before the scene is loaded after the screen is black
    private bool startedDelayedSceneLoading = false; // Has the delayed scene loading started
    private bool isTransitioning = false; // Is the game currently transitioning between scenes
    private EToState toState = EToState.MainMenu; // The state we are transitioning to

    private void Awake()
    {
        // Singleton pattern
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

    private void Start()
    {
        // Temp mouse lock
        Cursor.lockState = CursorLockMode.Locked;
        GameInitialize();
    }


    private void Update()
    {
        // Temp
        if(Input.GetKeyDown(KeyCode.O))
        {
            // Unlock the cursor
            Loose();
        }

        HandleTransition();
    }

    // ___ Other ___
    /// <summary>
    /// Used to determine what the reloaded scene should be.
    /// </summary>
    private enum EToState
    {
        MainMenu,
        LastCheckpoint,
    }
    private const EToState MAINMENU = EToState.MainMenu;
    private const EToState LASTCHECKPOINT = EToState.LastCheckpoint;


    // ___ Private methods ___
    private void ReloadScene(EToState _toState)
    {
        // We don't want to reload the scene if we are already transitioning
        if (isTransitioning)
            return;
        toState = _toState;
        isTransitioning = true;
        startedDelayedSceneLoading = false;
    }

    private void HandleTransition()
    {
        // Handle the fade effect
        fadeTimer += isTransitioning? Time.deltaTime : - Time.deltaTime;
        // Clamp the timer to be between 0 and fadeDuration
        fadeTimer = Mathf.Clamp(fadeTimer, 0, fadeDuration);
        fadeImage.color = Color.Lerp(Color.clear, Color.black, fadeTimer / fadeDuration);

        // Also fade the game audio

        if (fadeTimer >= fadeDuration && !startedDelayedSceneLoading)
        {
            // Start the delayed scene loading coroutine
            StartCoroutine(LoadSceneWithDelay());
            startedDelayedSceneLoading = true;
        }
    }

    private IEnumerator LoadSceneWithDelay()
    {
        // Wait for the specified delay time while the screen is black
        yield return new WaitForSeconds(blackScreenDelay);

        // Now reload scene
        SceneManager.LoadScene(0);
        StartCoroutine(StopTransitionWithDelay()); 

        // Now decide what to do after the scene is loaded
        if (toState == MAINMENU)
        {
            MainMenu(false);
        }
        else if (toState == LASTCHECKPOINT)
        {
            LoadLastCheckpoint(false);
        }
    }

    private IEnumerator StopTransitionWithDelay()
    {
        yield return new WaitForSeconds(5);

        isTransitioning = false;
    }

    private IEnumerator TeleportAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        // Teleport to the last checkpoint
        CheckpointManager.Instance.TeleportToLastCheckpoint();
    }

    // ___ Public methods ___

    /// <summary>
    /// When the game initially starts.
    /// </summary>
    public void GameInitialize()
    {
        if (isGameStarted)
            return;
        // Initialize the game
        CheckpointManager.Instance.OnGameInitialize(); // Initialize the checkpoint manager

        // Everything that needs to be done when the game initially starts
        MainMenu(false);

        isGameStarted = true;
    }

    /// <summary>
    /// Opens the main menu.
    /// </summary>
    public void MainMenu(bool reloadScene)
    {
        if (reloadScene)
        {
            ReloadScene(MAINMENU);
            return;
        }
        // Start anything that needs to be started when the main menu is opened
    }

    /// <summary>
    /// When pressed play.
    /// </summary>
    public void GameStart()
    {

    }

    /// <summary>
    /// When the player looses.
    /// </summary>
    public void Loose()
    {
        LoadLastCheckpoint(true); // Temp
    }

    public void LoadLastCheckpoint(bool reloadScene)
    {
        if (reloadScene)
        {
            ReloadScene(LASTCHECKPOINT);
            return;
        }
        // Teleport to the last checkpoint
        // Add a small delay to ensure player is initialized
        StartCoroutine(TeleportAfterDelay());
    }
}
