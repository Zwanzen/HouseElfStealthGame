using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using Pathfinding;
using RootMotion.FinalIK;
using SteamAudio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RigidbodyMovement;
using static SoundGameplayManager;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Rigidbody), typeof(Seeker))]
public class NPC : MonoBehaviour, IHear
{
    [Header("NPC Settings")]
    [field: SerializeField] private EventReference _reactionRef; 
    [SerializeField] private StudioEventEmitter _snoreEmitter;
    [SerializeField] private StudioEventEmitter _reactionEmitter;
    [SerializeField] private NPCType _npcType;
    [SerializeField] private Slider[] _slider; // 3 different sliders for different states'
    [SerializeField] private Image _detectedImage; // Used instead of the slider when detected
    [SerializeField] private NPCPath _path;
    [SerializeField] private Transform _eyes;

    [Space(10)] [Header("Movement Settings")] [SerializeField]
    private float _maxRecalcPathTime = 0.5f;
    [SerializeField] private float _lookAhead = 1f;
    [Space(5)]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private LayerMask _obstacleLayers;
    [SerializeField] private float _springStrength = 50f;
    [SerializeField] private float _springDamper = 5f;
    [SerializeField] private MovementSettings _settings;
    [SerializeField] private float _rotationSpeed = 100f;

    // ___ Private ___
    private Rigidbody _rigidbody;
    private Animator _anim;
    private FMODUnity.StudioEventEmitter _soundEmitter;
    private PlayerController _player;
    private Vector3 _soundPosOffset;
    private Vector3 _snorePosOffset;

    private Vector3 _startPos;

    // ___ NPC Specific ___
    private NPCMovement _movement;
    private NPCAnimator _animator;
    private NPCDetector _detector;
    
    public enum NPCType
    {
        Patrol,
        Sleep,
        Stationary,
    }

    // Properties
    public Rigidbody Rigidbody => _rigidbody;
    public Vector3 Position => _rigidbody.position;
    public Vector3 SoundPosition => _rigidbody.position + _soundPosOffset;
    public Vector3 SnorePosition => _rigidbody.position + _snorePosOffset;
    public NPCType Type => _npcType;
    public MovementSettings MovementSettings => _settings;
    public Vector3 EyesPos => _eyes.position;
    public Slider CurrentSlider { get;  private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _anim = GetComponentInChildren<Animator>();
        _soundEmitter = GetComponentInChildren<StudioEventEmitter>();

        // Used for stationary NPCs
        _startPos = transform.position;
        // Used to calculate the sound position
        _soundPosOffset = Position - _soundEmitter.transform.position;

        // If sleep
        if (_npcType == NPCType.Sleep)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            // Turn off the grounder
            GetComponentInChildren<GrounderFBBIK>().enabled = false;
            // Used for snore sound
            _snorePosOffset = Position - _snoreEmitter.transform.position;
        }
        CurrentSlider = _slider[0]; // Default to the first slider
    }

    private void Start()
    {
        InitializeNPC();

        if (_npcType == NPCType.Patrol)
            _movement.SetTarget(_path);
    }

    private void Update()
    {
        var delta = Time.deltaTime;
        _animator.Update(delta);
        _detector.Update(delta);
    }

    private void FixedUpdate()
    {
        var delta = Time.fixedDeltaTime;
        if(_npcType != NPCType.Sleep)
            _movement.FixedUpdate(delta);
    }
    
    private void InitializeNPC()
    {
        _player = PlayerController.Instance;
        _movement = new NPCMovement(this, _maxRecalcPathTime, _lookAhead, _groundLayers, _springStrength, _springDamper, _rotationSpeed);
        _animator = new NPCAnimator(this, _anim);

        _movement.ArrivedAtTarget += OnReachedTarget;
        _movement.OnAnimStateChange += OnAnimStateChange;

        _detector = new NPCDetector(this, _player, _obstacleLayers);
        _detector.OnDetectionStateChanged += OnDetectionStateChange;

        // If NPC is Sleeping, set the animation state to sleep
        if(_npcType == NPCType.Sleep)
            _animator.SetAnimStateForced(NPCAnimator.AnimState.Sleep);

    }

    private void OnReachedTarget()
    {
        // If we reached POI when NPC went looking for it
        if (_detector.DetectionState != NPCDetector.EDetectionState.Default)
            _detector.AtPointOfInterest();
    }

    private void OnAnimStateChange(NPCAnimator.AnimState state)
    {
        _animator.SetNewAnimState(state);
    }

    private NPCDetector.EDetectionState _lastDetectionState = NPCDetector.EDetectionState.Default;
    private void OnDetectionStateChange(NPCDetector.EDetectionState state)
    {
        if (_lastDetectionState != NPCDetector.EDetectionState.Curious
            && state == NPCDetector.EDetectionState.Curious)
        {
            // Sleeping NPCs will not move
            if (_npcType != NPCType.Sleep)
            {
                // Stop movement
                _movement.Stop();
                // After a few seconds, turn to the POI
                StartCoroutine(CheckOutPOI(state, Random.Range(1f, 1.5f)));
                // Play the curious sound
                PlayReactionSound(1);
            }
            // Set emotion curious
            _animator.SetEmotion(NPCAnimator.EmotionType.Curious);
            // Set the slider to the curious one
            UpdateDetectionUI(1);

            _lastDetectionState = state;
        }
        else if(_lastDetectionState!= NPCDetector.EDetectionState.Alert 
            && state == NPCDetector.EDetectionState.Alert)
        {

            // Sleeping NPCs will not move
            if (_npcType != NPCType.Sleep)
            {
                // Stop movement
                _movement.Stop();
                // After a few seconds, turn to the POI
                StartCoroutine(CheckOutPOI(state, Random.Range(1f, 1.5f)));
            }
            else
            {
                // Wake up the sleeping NPC
                _animator.SetNewAnimState(NPCAnimator.AnimState.SleepAlert);
                _snoreEmitter.Stop();

            }
            // Set emotion alert
            _animator.SetEmotion(NPCAnimator.EmotionType.Alert);
            // Set the slider to the alert one
            UpdateDetectionUI(2);
            // Play the alert sound
            PlayReactionSound(2);
            _lastDetectionState = state;
        }
        else if (state == NPCDetector.EDetectionState.Default)
        {
            // If previous state was curious, and type is not sleep, we want to play reaction sound
            if (_lastDetectionState == NPCDetector.EDetectionState.Curious && _npcType != NPCType.Sleep)
            {
                // Play the default sound
                PlayReactionSound(0);
            }
            // Set emotion normal
            _animator.SetEmotion(NPCAnimator.EmotionType.Normal);
            // Set the slider to the default one
            UpdateDetectionUI(0);

            _lastDetectionState = state;
            // Go back to default behavior
            if (_npcType == NPCType.Patrol)
            {
                _movement.SetTarget(_path);
            }
            else if (_npcType == NPCType.Stationary)
            {
                _movement.SetTarget(_startPos);
            }
            else
            {
                // Make the NPC go back to sleep
                _animator.SetNewAnimState(NPCAnimator.AnimState.Sleep);
            }
        }
    }

    private IEnumerator CheckOutPOI(NPCDetector.EDetectionState state,float delay)
    {
        yield return new WaitForSeconds(delay);
        // If we are curious, we want to turn to the POI
        if (state == NPCDetector.EDetectionState.Curious)
            _movement.SetTargetRotateTo(_detector.POI);
        // If we are alert, we want to move to the POI
        else if (state == NPCDetector.EDetectionState.Alert)
            _movement.SetTarget(_detector.POI);
    }

    private void PlayFootSound()
    {
        // Check colliders below the foot for tag
        var layer = _groundLayers;
        // Ignore Props
        layer &= ~(1 << LayerMask.NameToLayer("Props"));
        var amouont = Physics.SphereCast(transform.position + Vector3.up * 0.3f, 0.18f,Vector3.down, out var hit, 1f, layer);

        var tag = hit.collider.tag;
        var material = SoundGameplayManager.Instance.TryGetMaterialFromTag(tag);


        SoundGameplayManager.Instance.PlayGuardStep(_soundEmitter, material,SoundPosition);

    }

    private void PlayReactionSound(int index)
    {
        _reactionEmitter.EventReference = _reactionRef;
        _reactionEmitter.Play();
        _reactionEmitter.SetParameter("AlertLevel", index);
    }

    private void UpdateDetectionUI(int index)
    {
        //Deactivate all sliders
        foreach (var slider in _slider)
        {
            slider.gameObject.SetActive(false);
        }
        if (index == 0)
            return; // No need to activate the default slider
        index--; // Decrease the index to match the slider array (0,1,2)

        //Activate the selected slider
        _slider[index].gameObject.SetActive(true);
        CurrentSlider = _slider[index];

    }

    // Public Methods
    /// <summary>
    /// Called by the footstep animation to play the footstep sound.
    /// </summary>
    public void OnStepAnimation()
    {
        PlayFootSound();
    }

    /// <summary>
    /// Called by the sleep animation to play the snore sound.
    /// </summary>
    public void OnSnoreAnimation()
    {
        SoundGameplayManager.Instance.PlayGuardSnore(_snoreEmitter, SnorePosition);
    }

    /// <summary>
    /// Respond to a sound being made through the IHear interface.
    /// Send it to the detector.
    /// </summary>
    public void RespondToSound(Sound sound)
    {
        if(sound == null)
        {
            Debug.LogWarning("Sound is null");
            return;
        }
        _detector.OnSoundHeard(sound);
    }
}
