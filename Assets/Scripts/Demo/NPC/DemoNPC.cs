using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DemoNpc : MonoBehaviour
{
    [Header("Debug")] 
    [SerializeField] private bool _debugVisual = false;
    [SerializeField] private bool _enableDistanceFactor = true;
    
    [Space(10)]
    [Header("UI")]
    [SerializeField] private Slider _detectionSlider;
    
    [Space(10)]
    [Header("References")] 
    [SerializeField] private Transform _player;
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
    [SerializeField] private float _imageRate = 0.5f;
    [SerializeField] private int _reselution = 16;

    
    [SerializeField] private float _playerBrightness;
    
    private RenderTexture _renderTexture;
    private Texture2D _texture;
    
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
        _currentState = NpcState.Idle;
        InitializeLimbs();
        _fillReact = _detectionSlider.fillRect.GetComponent<Image>();
        
        // Light Stuff
        // Start with a random offset, Important for smooth gameplay
        _lightImageTimer = Random.Range(0, _imageRate);
        
        _renderTexture = new RenderTexture(_reselution, _reselution, 24);
        _lightCam.targetTexture = _renderTexture;

        _texture = new Texture2D(_reselution, _reselution, TextureFormat.RGBA32, false);
    }

    private void Update()
    {
        UpdateDetection();
        HandleUI();
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
    
    private float _decayTimer;

    private void UpdateDetection()
    {
        var detectionValue = 0f;
        
        
        // Add visual detection value
        detectionValue += Time.deltaTime * VisionDetection();
        
        // Decreaase the detection value over time if player is not being detected
        if (detectionValue <= 0)
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
        var limbs = _player.GetComponent<RigidbodyDemoController>().GetBodyParts();
        
        // If limbs are not set throw an error
        if (limbs.Length != 6)
        {
            Debug.LogError("NPC limbs are not set correctly. Please set the limbs in the inspector.");
            return;
        }
        
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
        if (Vector3.Distance(_player.position, transform.position) > _detectDistance)
            return 0;

        // Check if the player is outside the detection angle
        var directionToPlayer = (_player.position - transform.position).normalized;
        var angle = Vector3.Angle(_npcEyes.forward, directionToPlayer);
        if (angle > _detectAngle)
            return 0;
        
        // If we are past what's above, we are in the detection zone
        // We only now want to get light value, heavy computations
        HandleLightCamera();
        
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

        // Calculate the detection value
        return valueToAdd * GetDistanceMultiplier() * _visionMultiplier * _detectionSpeed * GetLightMultiplier();
    }

    private Image _fillReact;
    
    private void HandleUI()
    {
    #if UNITY_EDITOR
        // Only enable the slider if this gameObject is selected in the editor
        if (_detectionSlider != null)
        {
            bool isSelected = UnityEditor.Selection.activeGameObject == this.gameObject;
            _detectionSlider.gameObject.SetActive(isSelected);
            
            // Update the slider value when active
            if (isSelected)
            {
                // Update the color based on the value
                var color = Color.Lerp(Color.green, Color.red, _detection / 100f);
                _fillReact.color = color;
                _detectionSlider.value = _detection / 100f;
            }
        }
    #endif
    }
    
    private float _lightImageTimer;
    
    private void HandleLightCamera()
    {
        _lightImageTimer += Time.deltaTime;
        if (_lightImageTimer < _imageRate) { return;}
        _lightImageTimer = 0;

        // Turn on the light camera
        _lightCam.gameObject.SetActive(true);
        
        Vector3 direction = _player.position - _lightCam.transform.position;
        direction.Normalize();

        Quaternion toRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        _lightCam.transform.rotation = toRotation;

        _playerBrightness = ColorIntensity(true);
        // Turn off the light camera
        _lightCam.gameObject.SetActive(false);
    }

    private float _silhouetteImageTimer;
    
    private void HandleSilhouetteCamera()
    {
        
    }
    
    private float ColorIntensity(bool ignoreColor = false)
    {
        _lightCam.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = _renderTexture;
        _texture.ReadPixels(new Rect(0, 0, _reselution, _reselution), 0, 0);
        _texture.Apply();


        float brightness = 0;   
        int count = 0;

        // Should Find non-Workaround for this
        NativeArray<Color32> pixels = new NativeArray<Color32>(_texture.GetPixelData<Color32>(0), Allocator.TempJob);
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
        _renderTexture.Release();

        return brightness /= count;
    }
    
}
