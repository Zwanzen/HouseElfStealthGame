using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using Pathfinding;
using SteamAudio;
using UnityEngine;
using UnityEngine.UI;
using static RigidbodyMovement;
using static SoundGameplayManager;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Rigidbody), typeof(Seeker))]
public class NPC : MonoBehaviour, IHear
{
    [SerializeField] private bool DebugMode = false;

    [Header("NPC Settings")]
    [SerializeField] private NPCType _npcType;
    [SerializeField] private Slider _slider;
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

    private Vector3 _startPos;

    // ___ NPC Specific ___
    private NPCMovement _movement;
    private NPCAnimator _animator;
    private NPCDetector _detector;
    
    private enum NPCType
    {
        Patrol,
        Sleep,
        Stationary,
    }

    // Properties
    public Rigidbody Rigidbody => _rigidbody;
    public Vector3 Position => _rigidbody.position;
    public MovementSettings MovementSettings => _settings;
    public Vector3 EyesPos => _eyes.position;

    SteamAudioListener listener;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _anim = GetComponentInChildren<Animator>();
        _soundEmitter = GetComponentInChildren<StudioEventEmitter>();

        listener = GetComponentInChildren<SteamAudioListener>();

        // Used for stationary NPCs
        _startPos = transform.position;
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
        _movement.FixedUpdate(delta);
    }
    
    private void InitializeNPC()
    {
        _player = PlayerController.Instance;
        _movement = new NPCMovement(this, _maxRecalcPathTime, _lookAhead, _groundLayers, _springStrength, _springDamper, _rotationSpeed);
        _animator = new NPCAnimator(this, _anim);

        _movement.ArrivedAtTarget += OnReachedTarget;
        _movement.OnAnimStateChange += OnAnimStateChange;

        _detector = new NPCDetector(this, _player, _slider, _obstacleLayers);
        _detector.OnDetectionStateChanged += OnDetectionStateChange;
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
            // Stop movement
            _movement.Stop();
            // After a few seconds, turn to the POI
            StartCoroutine(CheckOutPOI(state, Random.Range(0.5f, 1.5f)));
            _lastDetectionState = state;
        }
        else if(_lastDetectionState!= NPCDetector.EDetectionState.Alert 
            && state == NPCDetector.EDetectionState.Alert)
        {
            // Stop movement
            _movement.Stop();
            // After a few seconds, move to the POI
            StartCoroutine(CheckOutPOI(state, Random.Range(0.5f, 1.5f)));
            _lastDetectionState = state;
        }
        else if (state == NPCDetector.EDetectionState.Default)
        {
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

    private RaycastHit[] _stepColliders = new RaycastHit[10];
    private void PlayFootSound()
    {
        // Check colliders below the foot for tag
        var amouont = Physics.SphereCastNonAlloc(transform.position + Vector3.up * 0.3f, 0.18f,Vector3.down, _stepColliders, 1f, _groundLayers);

        // Loop through and return the first working tag
        var material = EMaterialTag.None;
        for (int i = 0; i < amouont; i++)
        {
            var tag = _stepColliders[i].collider.tag;
            if (tag == "Untagged")
                continue;
            var m = SoundGameplayManager.Instance.TryGetMaterialFromTag(tag);
            if (m == EMaterialTag.None)
                continue;
            else
                material = m;
        }

        SoundGameplayManager.Instance.PlayGuardStep(_soundEmitter, material);

    }

    // Public Methods
    public void OnStepAnimation()
    {
        PlayFootSound();
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
