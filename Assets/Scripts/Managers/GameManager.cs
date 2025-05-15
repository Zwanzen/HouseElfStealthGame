using FMODUnity;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class is responsible for managing the game state,
/// loading scenes, menues, and so on.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private Animator menuAnimator; // The animator for all menus
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Image fadeImage; // The image that fades the screen to black when reloading the scene

    [Header("Settings")]
    [SerializeField] private Slider volumeSlider; // The slider for the volume
    [SerializeField] private Slider sensitivitySlider; // The slider for the sensitivity
    [SerializeField] private Slider brightnessSlider; // The slider for the brightness

    // v2
    [SerializeField] private Slider volumeSlider2;
    [SerializeField] private Slider sensitivitySlider2;
    [SerializeField] private Slider brightnessSlider2;

    private float _storedVolume = 0.5f; // The stored volume
    private float _storedSensitivity = 0.5f; // The stored sensitivity
    private float _storedBrightness = 0.5f; // The stored brightness

    // For MenuAnimator
    private struct MenuTrigger
    {
        public static readonly int Pause = Animator.StringToHash("Pause");
        public static readonly int Nothing = Animator.StringToHash("Nothing");
        public static readonly int MainMenu = Animator.StringToHash("MainMenu");
        public static readonly int MainMenuSettings = Animator.StringToHash("MainMenuToSettings");
        public static readonly int SettingsMainMenu = Animator.StringToHash("SettingsToMainMenu");
        public static readonly int PauseSettings = Animator.StringToHash("PauseToSettings");
        public static readonly int SettingsPause = Animator.StringToHash("SettingsToPause");

    }

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
    private bool isMainMenu = false; // Is the main menu currently open

    private InputManager inputManager; // Reference to the input manager

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
        GameInitialize();

        // Get the input manager
        inputManager = InputManager.Instance;
    }


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }

        HandleTransition();
        HandleSettings();
    }

    // ___ Other ___
    /// <summary>
    /// Used to determine what the reloaded scene should be.
    /// </summary>
    private enum EToState
    {
        MainMenu,
        GameStart,
        LastCheckpoint,
    }
    private const EToState MAINMENU = EToState.MainMenu;
    private const EToState LASTCHECKPOINT = EToState.LastCheckpoint;
    private const EToState GAMESTART = EToState.GameStart;


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
        fadeTimer += isTransitioning? Time.unscaledDeltaTime : - Time.unscaledDeltaTime;
        // Clamp the timer to be between 0 and fadeDuration
        fadeTimer = Mathf.Clamp(fadeTimer, 0, fadeDuration);
        fadeImage.color = Color.Lerp(Color.clear, Color.black, fadeTimer / fadeDuration);

        // Also fade the game audio


        if (fadeTimer >= fadeDuration && !startedDelayedSceneLoading)
        {
            // Make sure we are resumed
            Resume();

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
        else if (toState == GAMESTART)
        {
            GameStart(false);
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
        isMainMenu = true;
        // Turn off player
        PlayerController.Instance.gameObject.SetActive(false);
        // Turn on menu camera
        menuCamera.gameObject.SetActive(true);
        // Turn on the menu 
        menuAnimator.SetTrigger(MenuTrigger.MainMenu);
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Pause the game.
    /// </summary>
    public void Pause()
    {
        if(isMainMenu || isTransitioning)
            return;

        // Pause the game
        Time.timeScale = 0;
        // Unlock the cursor
        Cursor.lockState = CursorLockMode.None;
        // Disable the player input
        inputManager.DisableInputs();
        // Show the pause menu
        menuAnimator.SetTrigger(MenuTrigger.Pause);
    }

    /// <summary>
    /// Resume the game.
    /// </summary>
    public void Resume()
    {
        // Resume the game
        Time.timeScale = 1;
        // Lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        // Enable the player input
        inputManager.EnableInputs();
        // Hide the pause menu
        menuAnimator.SetTrigger(MenuTrigger.Nothing);
    }

    /// <summary>
    /// When pressed play.
    /// </summary>
    public void GameStart(bool reloadScene)
    {
        if (reloadScene)
        {
            ReloadScene(GAMESTART);
            return;
        }
        isMainMenu = false;
        // Set animator to nothing
        menuAnimator.SetTrigger(MenuTrigger.Nothing);
        // Turn off menu camera
        menuCamera.gameObject.SetActive(false);

    }

    /// <summary>
    /// When the player looses.
    /// </summary>
    public void GameOver()
    {
        LoadLastCheckpoint(true); // Temp
    }

    /// <summary>
    /// When the player wins.
    /// </summary>
    public void Win()
    {

    }

    /// <summary>
    /// When the player wants to exit the game.
    /// </summary>
    public void ExitGame()
    {
        // Exit the game
        Application.Quit();
    }

    private void UpdateSliders(bool isMenu)
    {
        // Update the sliders with the stored values
        if (isMenu)
        {
            volumeSlider.value = _storedVolume;
            sensitivitySlider.value = _storedSensitivity;
            brightnessSlider.value = _storedBrightness;
        }
        else
        {
            volumeSlider2.value = _storedVolume;
            sensitivitySlider2.value = _storedSensitivity;
            brightnessSlider2.value = _storedBrightness;
        }
    }

    private bool isSettings;

    private void HandleSettings()
    {
        if(!isSettings)
            return;

        if (isMainMenu)
        {
            _storedBrightness = brightnessSlider.value;
            _storedSensitivity = sensitivitySlider.value;
            _storedVolume = volumeSlider.value;
        }
        else
        {
            _storedBrightness = brightnessSlider2.value;
            _storedSensitivity = sensitivitySlider2.value;
            _storedVolume = volumeSlider2.value;
        }
    }

    public void GoSettings(bool isMenu)
    {
        // Find out if we are in the main menu or in the game
        if (isMenu)
        {
            // Go to the settings menu
            menuAnimator.SetTrigger(MenuTrigger.MainMenuSettings);
            UpdateSliders(true);
        }
        else
        {
            // Go to the settings menu
            menuAnimator.SetTrigger(MenuTrigger.PauseSettings);
            UpdateSliders(false);
        }

        isSettings = true;
    }

    public void LeaveSettings(bool isMenu)
    {
        // Find out if we are in the main menu or in the game
        if (isMenu)
        {
            // Go to the settings menu
            menuAnimator.SetTrigger(MenuTrigger.SettingsMainMenu);
        }
        else
        {
            // Go to the settings menu
            menuAnimator.SetTrigger(MenuTrigger.SettingsPause);
        }

        isSettings = false;
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
