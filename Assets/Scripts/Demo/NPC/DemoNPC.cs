using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DemoNpc : MonoBehaviour, IHear
{
    [Header("Debug")] 
    [SerializeField] private bool _enableSoundReaction = true;
    [SerializeField] private bool _debugVisual = false;
    [SerializeField] private bool _enableDistanceFactor = true;
    [SerializeField] private bool _enableTurning = false;
    
    [SerializeField] private Image _lightImage;
    [SerializeField] private Image _backgroundImage;
    private TextMeshProUGUI _lightText;
    private TextMeshProUGUI _backgroundText;
    
    [Space(10)]
    [Header("UI")]
    [SerializeField] private Slider _detectionSlider;
    
    [Space(10)]
    [Header("References")] 
    [SerializeField] private PlayerController _player;
    [SerializeField] private Transform _npcEyes;
    [SerializeField] private Camera _lightCam;
    [SerializeField] private Camera _silhouetteCam;

    [Space(10)] 
    [Header("Common")] 
    [SerializeField] private LayerMask _obstacleLayer;
    
    private Transform _head;
    private Transform _leftHand;
    private Transform _rightHand;
    private Transform _leftLeg;
    private Transform _rightLeg;
    private Transform _body;
    
    [Space(10)]
    [Header("Detection Settings")]
    [SerializeField] private float _detectionSpeed = 1f;
    [SerializeField] private float _detectionDecay = 0.5f;
    [SerializeField] private float _detectionDecayDelay = 1f;
    
    [Space(4)]
    [Header("Vision Detection")]
    [SerializeField] private float _detectDistance = 10f;
    [SerializeField] private float _detectAngle = 45f;
    [SerializeField] private AnimationCurve _angleCurve;
    [SerializeField] private float _angleDetectionMultiplier = 1f;
    [SerializeField] private AnimationCurve _distanceCurve;
    [SerializeField] private float _distanceDetectionMultiplier = 1f;
    [Space(2)]
    [SerializeField] private float _visionMultiplier = 1f;
    [Space(2)]
    [SerializeField] private float _headValue = 0.2f;
    [SerializeField] private float _handValue = 0.1f;
    [SerializeField] private float _legValue = 0.1f;
    [SerializeField] private float _bodyValue = 0.4f;

    [Space(10)]
    [Header("Light Detection")]
    [SerializeField] private AnimationCurve _lightCurve;
    [SerializeField] private float _lightDetectionMultiplier = 1f;
    [Space(2)]
    [SerializeField] private AnimationCurve _backgroundCurve;
    [SerializeField] private float _backgroundDetectionMultiplier = 1f;
    [Space(2)]
    [SerializeField] private float _imageRate = 0.5f;
    [SerializeField] private int _reselution = 16;
    
    [Space(10)]
    [Header("Auditory Detection")]
    [SerializeField] private float _soundDecayRate = 0.5f;
    [SerializeField] private AnimationCurve _audioDistanceCurve;
    [SerializeField] private float _audioDistanceMultiplier = 1f;

    
    [SerializeField] private float _playerBrightness;
    [SerializeField] private float _backgroundBrightness;
    
    private RenderTexture _lightRenderTexture;
    private RenderTexture _silhouetteRenderTexture;
    private Texture2D _lightTexture;
    private Texture2D _silhouetteTexture;
    
    private bool _hasIgnored;
    private float _ignoreRed;
    private float _ignoreGreen;
    private float _ignoreBlue;
    
    private float _detection;
    private NpcState _currentState;
    
    public enum NpcState
    {
        Idle,
        Patrol,
        Detected
    }
    
    private void Awake()
    {
        _heardSounds = new List<Sound>();
        
        _currentState = NpcState.Idle;
        InitializeLimbs();
        _fillReact = _detectionSlider.fillRect.GetComponent<Image>();
        
        // Light Stuff
        // Start with a random offset, Important for smooth gameplay
        _lightImageTimer = Random.Range(0, _imageRate);
        
        // Create the render texture
        _lightRenderTexture = new RenderTexture(_reselution, _reselution, 24, RenderTextureFormat.ARGB32,0);
        _lightRenderTexture.filterMode = FilterMode.Point;
        // Create the render texture
        _silhouetteRenderTexture = new RenderTexture(_reselution, _reselution, 24, RenderTextureFormat.ARGB32,0);
        _silhouetteRenderTexture.filterMode = FilterMode.Point;
        
        _lightCam.targetTexture = _lightRenderTexture;
        _silhouetteCam.targetTexture = _silhouetteRenderTexture;

        _lightTexture = new Texture2D(_reselution, _reselution, TextureFormat.RGBA32, false, true);
        _lightTexture.filterMode = FilterMode.Point;
        _silhouetteTexture = new Texture2D(_reselution, _reselution, TextureFormat.RGBA32, false, true);
        _silhouetteTexture.filterMode = FilterMode.Point;
        _lightText = _lightImage.GetComponentInChildren<TextMeshProUGUI>();
        _backgroundText = _backgroundImage.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        UpdateDetection();
        HandleUI();

        if (_enableTurning)
        {
            RotateToPlayer();
        }
        
        UpdateStoredSounds();
    }

    private void RotateToPlayer()
    {
        if (_detection > 50f)
        {
            var pPos = _player.Position;
            var tPos = transform.position;
            pPos.y = 0;
            tPos.y = 0;
            var dir = pPos - tPos;
            dir.Normalize();
            var toRotation = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = toRotation;
        }
    }

    private List<Sound> _heardSounds;
    
    public void RespondToSound(Sound sound)
    {
        // For debugging purposes
        if(!_enableSoundReaction)
            return;
        
        // If it is a looping sound
        if (_heardSounds.Contains(sound))
            return;
        
        var soundCurrentVolume = GetSoundCurrentVolume(sound);
        if(soundCurrentVolume <= 0)
            return;
        
        // Find out if there is a sound louder than the current one
        var shouldAdd = true;
        if (_heardSounds.Count > 0)
        {
            foreach (var s in _heardSounds)
            {
                if(s.CurrentVolume > soundCurrentVolume)
                {
                    shouldAdd = false;
                    break;
                }
            }
        }
        if (!shouldAdd) return;
            
        sound.CurrentVolume = soundCurrentVolume;
        _heardSounds.Add(sound);
        if (sound.SoundType == Sound.ESoundType.Player)
        {
            UpdateDetection(soundCurrentVolume);
        }
    }
    
    private float GetSoundCurrentVolume(Sound sound)
    {
        // Find the amount the detection value should be increased
        var soundDistance = Vector3.Distance(sound.Pos, transform.position);
        var lerpValue = soundDistance / sound.Range;
        var soundDetectionValue = sound.Amplitude * _audioDistanceCurve.Evaluate(lerpValue) * _audioDistanceMultiplier;
        
        return soundDetectionValue;
    }
    
    private void UpdateStoredSounds()
    {
        if(_heardSounds.Count == 0)
            return;
        
        // Create a temporary list to store sounds that need to be removed
        List<Sound> soundsToRemove = new List<Sound>();

        foreach (var s in _heardSounds)
        {
            if (s.Loop)
            {
                // Update the sound volume
                s.CurrentVolume = GetSoundCurrentVolume(s);
            }
            else
            {
                // Decrease the sound volume over time
                s.CurrentVolume -= Time.deltaTime * _soundDecayRate;
            }

            
            // Check if the sound is still valid
            if (s.CurrentVolume <= 0)
            {
                soundsToRemove.Add(s);
            }
        }
        
        // Now remove all the sounds in the removal list
        foreach (var sound in soundsToRemove)
        {
            _heardSounds.Remove(sound);
        }

    }

    private float GetDistanceMultiplier()
    {
        // Check if the distance factor is enabled
        if(!_enableDistanceFactor)
            return 1f;
        
        // Calculate the distance between the player and the NPC
        var distance = Vector3.Distance(_player.transform.position, transform.position);
        // Calculate based on a curve, exponentially increasing the value the closer the player is
        var evaluatedValue = _distanceCurve.Evaluate(distance / _detectDistance);
        // Return the multiplied evaluated value
        return evaluatedValue * _distanceDetectionMultiplier;
    }
    
    private float GetLightMultiplier()
    {
        // Calculate the brightness of the player
        var evaluatedValue = _lightCurve.Evaluate(_playerBrightness);
        // Return the multiplied evaluated value
        return evaluatedValue * _lightDetectionMultiplier;
    }

    private float GetBackgroundMultiplier()
    {
        // Calculate the brightness of the background
        var evaluatedValue = _backgroundCurve.Evaluate(_backgroundBrightness);
        // Return the multiplied evaluated value
        return evaluatedValue * _backgroundDetectionMultiplier;
    }
    
    private float _decayTimer;

    private void UpdateDetection(float newValue = 0f)
    {
        var detectionValue = newValue;
        
        
        // Add visual detection value
        detectionValue += Time.deltaTime * VisionDetection();
        
        // Decreaase the detection value over time if player is not being detected
        if (detectionValue <= 0.5f)
        {
            _decayTimer += Time.deltaTime;
            // If the decay timer is greater than the decay delay, start decaying the detection value
            if (_decayTimer > _detectionDecayDelay)
            {
                detectionValue -= Time.deltaTime * _detectionDecay;
            }
        }
        else
        {
            _decayTimer = 0;
        }
        
        
        _detection += detectionValue;
        
        // Clamp the detection value between 0 and 100
        _detection = Mathf.Clamp(_detection, 0, 100);
    }

    private void InitializeLimbs()
    {
        var limbs = _player.Limbs;
        
        _head = limbs[0];
        _leftHand = limbs[1];
        _rightHand = limbs[2];
        _leftLeg = limbs[3];
        _rightLeg = limbs[4];
        _body = limbs[5];
    }
    
    private bool LimbVisible(Transform limb)
    {
        if (_debugVisual)
        {
            // Set color red if the limb is not visible
            var lineColor = !Physics.Linecast(_npcEyes.position,limb.position, _obstacleLayer) ? Color.green : Color.red;
            Debug.DrawLine(_npcEyes.position, limb.position, lineColor);
        }
        return !Physics.Linecast(_npcEyes.position,limb.position, _obstacleLayer);
    }
    
    private float VisionDetection()
    {
        // Check if the player is outside the detection distance
        if (Vector3.Distance(_player.Position, transform.position) > _detectDistance)
            return 0;

        // Check if the player is outside the detection angle
        var directionToPlayer = (_player.Position - transform.position).normalized;
        var angle = Vector3.Angle(_npcEyes.forward, directionToPlayer);
        if (angle > _detectAngle)
            return 0;
        
        // If we are past what's above, we are in the detection zone
        
        // Linecast to each limb, and add the correlating detection value
        var valueToAdd = 0f;
        // Head
        valueToAdd += LimbVisible(_head) ? _headValue : 0;
        // Hands
        valueToAdd += LimbVisible(_leftHand) ? _handValue : 0;
        valueToAdd += LimbVisible(_rightHand) ? _handValue : 0;
        // Legs
        valueToAdd += LimbVisible(_leftLeg) ? _legValue : 0;
        valueToAdd += LimbVisible(_rightLeg) ? _legValue : 0;
        // Body
        valueToAdd += LimbVisible(_body) ? _bodyValue : 0;
        
        // We only now want to get light value, heavy computations
        if(valueToAdd == 0)
            return 0;
        HandleLightCamera();

        // Calculate the detection value
        return valueToAdd * GetDistanceMultiplier() * _visionMultiplier * _detectionSpeed * (GetLightMultiplier() + GetBackgroundMultiplier());
    }

    private Image _fillReact;
    
    private void HandleUI()
    {
        // Update the color based on the value
        var color = Color.Lerp(Color.green, Color.red, _detection / 100f);
        _fillReact.color = color;
        _detectionSlider.value = _detection / 100f;
        
        // If the this is selected, show the images
        #if UNITY_EDITOR
            bool isSelected = UnityEditor.Selection.activeGameObject == this.gameObject;
            if (isSelected)
            {
                _lightImage.sprite = Sprite.Create(_lightTexture, new Rect(0, 0, _reselution, _reselution), Vector2.zero);
                _backgroundImage.sprite = Sprite.Create(_silhouetteTexture, new Rect(0, 0, _reselution, _reselution), Vector2.zero);
                _lightText.text = "Light: " + _playerBrightness.ToString("F2");
                _backgroundText.text = "Background: " + _backgroundBrightness.ToString("F2");
            }

        #endif
    }
    
    private float _lightImageTimer;
    
    private void HandleLightCamera()
    {
        _lightImageTimer += Time.deltaTime;
        if (_lightImageTimer < _imageRate) { return;}
        _lightImageTimer = 0;

        // Light detection
        // Turn on the light camera
        
        _lightCam.enabled = true;
        
        var playerPos = _player.Position - Vector3.up * 0.5f;
        Vector3 direction = (playerPos) - _lightCam.transform.position;
        direction.Normalize();

        Quaternion toRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        _lightCam.transform.rotation = toRotation;

        _playerBrightness = ColorIntensity(_lightCam, _lightRenderTexture, _lightTexture,true);
        // Turn off the light camera
        _lightCam.enabled = false;
        
        // Background detection
        // Turn on the silhouette camera
        _silhouetteCam.enabled = true;
        // Set the camera to look at the player
        direction = playerPos - _silhouetteCam.transform.position; 
        toRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        _silhouetteCam.transform.rotation = toRotation;
        
        // Set the clipping plane to the player
        _silhouetteCam.nearClipPlane = Vector3.Distance(_silhouetteCam.transform.position, playerPos);
        
        _backgroundBrightness = ColorIntensity(_silhouetteCam, _silhouetteRenderTexture, _silhouetteTexture, false);
        // Turn off the silhouette camera
        _silhouetteCam.enabled = false;
    }
    
    private float ColorIntensity(Camera cam, RenderTexture rt,Texture2D tex, bool ignoreColor = false)
    {
        cam.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _reselution, _reselution), 0, 0);
        tex.Apply();
        
        float brightness = 0;   
        int count = 0;

        // Should Find non-Workaround for this
        NativeArray<Color32> pixels = new NativeArray<Color32>(tex.GetPixelData<Color32>(0), Allocator.TempJob);
        NativeArray<Color> colors = new NativeArray<Color>(pixels.Length, Allocator.TempJob);
        for (int i = 0; i < pixels.Length; i++)
        {
            colors[i] = pixels[i];
        }

        // Check the MAX value of every color, this gives a better result because
        // technically the brightest red color is not as bright as the brightest green color.
        // This rather looks at how shaded/bright the color is.
        for (int i = 0; i < colors.Length; i++)
        {
            // Ignore color functionality
            if (ignoreColor)
            {
                // If ignored color not set, set it
                if (!_hasIgnored)
                {
                    _ignoreRed = colors[0].r;
                    _ignoreGreen = colors[0].g;
                    _ignoreBlue = colors[0].b;
                    _hasIgnored = true;
                }
                
                // If the color is ignored, skip it
                if (Mathf.Approximately(colors[i].r, _ignoreRed) && Mathf.Approximately(colors[i].g, _ignoreGreen) && Mathf.Approximately(colors[i].b, _ignoreBlue))
                    continue;
            }
            
            
            float max = Mathf.Max(colors[i].r, colors[i].g, colors[i].b);

            brightness += max;
            count++;
        }

        // Important to dispose of the NativeArray for memory management
        pixels.Dispose();
        colors.Dispose();
        RenderTexture.active = previous;
        rt.Release();

        return brightness /= count;
    }
    
}
