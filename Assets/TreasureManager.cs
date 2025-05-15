using UnityEngine;
using UnityEngine.UI;

public class TreasureManager : MonoBehaviour
{
    [SerializeField] private Image _treasureImage;
    [SerializeField] private GameObject[] _visuals;
    [SerializeField] private GameObject _hat;
    private bool _playerInRange;
    private bool _isCollected;
    private Animator _animator;

    private void Awake()
    {
        _treasureImage.gameObject.SetActive(false);
        _animator = GetComponent<Animator>();
        ToggleVisuals(false);
    }

    private void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.E) && !_isCollected)
        {
            // Assuming you have a method to handle the treasure collection
            CollectTreasure();
        }
    }

    private void ToggleVisuals(bool active)
    {
        foreach (var visual in _visuals)
        {
            visual.SetActive(active);
        }
    }

    private void CollectTreasure()
    {
        _treasureImage.gameObject.SetActive(false);
        GameManager.Instance.CollectTreasure();
        ToggleVisuals(true);
        _animator.SetTrigger("Collect");
        PlayerController.Instance.ToggleUI(false);
    }

    public void OnTreasureCollected()
    {
        GameManager.Instance.TreasureCollected();
        _isCollected = true;
        ToggleVisuals(false);
        PlayerController.Instance.ToggleUI(true);
        _hat.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isCollected)
        {
            _playerInRange = true;
            // Show the treasure image or UI element
            _treasureImage.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            // Hide the treasure image or UI element
            _treasureImage.gameObject.SetActive(false);
        }
    }
}
