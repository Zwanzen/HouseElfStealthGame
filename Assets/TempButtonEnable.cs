using UnityEngine;

public class TempButtonEnable : MonoBehaviour
{
    [SerializeField] private KeyCode _key = KeyCode.L;
    [SerializeField] private Transform _objectToToggle;


    private void Update()
    {
        if (Input.GetKeyDown(_key))
        {
            ToggleObject();
        }
    }
    
    private void ToggleObject()
    {
        if (_objectToToggle != null)
        {
            _objectToToggle.gameObject.SetActive(!_objectToToggle.gameObject.activeSelf);
        }
        else
        {
            Debug.LogWarning("No object assigned to toggle.");
        }
    }
}
