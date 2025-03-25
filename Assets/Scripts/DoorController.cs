using System;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _openClip;
    [SerializeField] private AudioClip _closeClip;
    
    [Space(20f)]
    [Header("Components")]
    [SerializeField] HingeJoint _hinge; // Rotasjonspunktet for d�ren
    private float closeThreshold = 10f; // Grense for automatisk lukking
    
    private Rigidbody _rigidbody;
    private bool _isGrabbed;
    private bool _isClosed;
    
    //Read only properties
    public Rigidbody Rigidbody => _rigidbody;
    public HingeJoint Hinge => _hinge;

    private void Awake()
    {
        _rigidbody = _hinge.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if(!_isClosed && !_isGrabbed)
        {
            CheckClose(); // Sjekker om d�ren skal lukkes automatisk
        }
    }

    public void OnGrabDoor()
    {
        Debug.Log("Grabbed door");
        _isGrabbed = true;
        _audioSource.clip = _openClip;
        _audioSource.Play();
        var sound = new Sound(transform.position, 2f, 3f)
        {
            SoundType = Sound.ESoundType.Player
        };
        Sounds.MakeSound(sound);
        if (_isClosed)
        {
            _isClosed = false;
            _hinge.limits = new JointLimits
            {
                min = -90,
                max = 90
            }; // Setter grensen for hvor mye d�ren kan �pnes
        }
    }

    public void OnReleaseDoor()
    {
        Debug.Log("Released door");
        _isGrabbed = false;
    }
    
    private void HandleDoor()
    {
        /*
        float mouseX = Input.GetAxis("Mouse X"); // Henter musebevegelse horisontalt
        Vector3 newRotation = hinge.localEulerAngles;
        newRotation.y += mouseX * rotationSpeed; // Roterer kun rundt Y-aksen
        hinge.localEulerAngles = newRotation;
        */
    }

    private void CheckClose()
    {
        float angle = Mathf.Abs(Mathf.DeltaAngle(_hinge.transform.localEulerAngles.y, 0)); // Beregner reell vinkel til 0 (lukket)
        if (angle < closeThreshold) // Hvis d�ren er nesten lukket
        {
            Close(); // Lukk d�ren
        }
    }

    private void Close()
    {
        _isClosed = true;
        _audioSource.clip = _closeClip;
        _audioSource.Play();
        var sound = new Sound(transform.position, 5f, 10f)
        {
            SoundType = Sound.ESoundType.Player
        };
        Sounds.MakeSound(sound);
        _hinge.limits = new JointLimits
        {
            min = 0,
            max = 0
        }; // Setter grensen for hvor mye d�ren kan �pnes
    }
}
