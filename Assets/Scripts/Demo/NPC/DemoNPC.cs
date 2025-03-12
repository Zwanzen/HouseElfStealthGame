using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DemoNPC : MonoBehaviour
{
    [Header("Debug")] 
    [SerializeField] private bool _debugVisual = false;
    
    [Space(10)]
    [Header("UI")]
    [SerializeField] private Slider _detectionSlider;
    
    [Space(10)]
    [Header("References")] 
    [SerializeField] private RigidbodyDemoController _player;
    [SerializeField] private Transform _npcEyes;

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
    [SerializeField] private AnimationCurve _distanceCurve;
    [SerializeField] private float _distanceDetectionValue = 1f;
    [Space(2)]
    [SerializeField] private float _visionMultiplier = 1f;
    [Space(2)]
    [SerializeField] private float _headValue = 0.2f;
    [SerializeField] private float _handValue = 0.1f;
    [SerializeField] private float _legValue = 0.1f;
    [SerializeField] private float _bodyValue = 0.4f;

    private float _detection;
    
    private NPCState _currentState;
    
    public enum NPCState
    {
        Idle,
        Patrol,
        Detected
    }
    
    private void Awake()
    {
        _currentState = NPCState.Idle;
        InitializeLimbs();
        _fillReact = _detectionSlider.fillRect.GetComponent<Image>();
    }

    private void Update()
    {
        UpdateDetection();
        HandleUI();
    }

    private float GetDistanceMultiplier()
    {
        // Calculate the distance between the player and the NPC
        var distance = Vector3.Distance(_player.transform.position, transform.position);
        // Calculate based on a curve, exponentially increasing the value the closer the player is
        var evaluatedValue = _distanceCurve.Evaluate(distance / _detectDistance);
        // Return the multiplied evaluated value
        return evaluatedValue * _distanceDetectionValue;
    }
    
    private float _decayTimer;

    private void UpdateDetection()
    {
        var detectionValue = 0f;
        
        
        // Add visual detection value
        detectionValue += Time.deltaTime * VisionDetection();
        
        // Decreaase the detection value over time if player is not being detected
        if (detectionValue == 0)
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
        var limbs = _player.GetBodyParts();
        
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
        if (Vector3.Distance(_player.transform.position, transform.position) > _detectDistance)
            return 0;

        // Check if the player is outside the detection angle
        var directionToPlayer = (_player.transform.position - transform.position).normalized;
        var angle = Vector3.Angle(_npcEyes.forward, directionToPlayer);
        if (angle > _detectAngle)
            return 0;
        
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
        return valueToAdd * GetDistanceMultiplier() * _visionMultiplier * _detectionSpeed;
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
    
}
