using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform hinge; // Rotasjonspunktet for døren
    private bool isGrabbed = false; // Sjekker om spilleren kontrollerer døren
    private Quaternion closedRotation; // Startrotasjonen til døren
    private float closeThreshold = 10f; // Grense for automatisk lukking
    public float rotationSpeed = 5f; // Hvor raskt døren roteres med musa
    [SerializeField] private Rigidbody _rigidbody;

    //Read only properties
    public Rigidbody Rigidbody => _rigidbody;

    private void Start()
    {
        closedRotation = Quaternion.Euler(0, 0, 0); // Lukket posisjon = 0 grader
    }

    private void Update()
    {
        // Trykk F for å toggle dørkontroll
        if (Input.GetKeyDown(KeyCode.F))
        {
            isGrabbed = !isGrabbed; // Bytter mellom å kontrollere eller ikke
        }

        if (isGrabbed)
        {
            HandleDoor(); // Lar spilleren rotere døren med musa
        }
        else
        {
            CheckClose(); // Sjekker om døren skal lukkes automatisk
        }
    }

    private void HandleDoor()
    {
        float mouseX = Input.GetAxis("Mouse X"); // Henter musebevegelse horisontalt
        Vector3 newRotation = hinge.localEulerAngles;
        newRotation.y += mouseX * rotationSpeed; // Roterer kun rundt Y-aksen
        hinge.localEulerAngles = newRotation;
    }

    private void CheckClose()
    {
        float angle = Mathf.Abs(Mathf.DeltaAngle(hinge.localEulerAngles.y, 0)); // Beregner reell vinkel til 0 (lukket)
        if (angle < closeThreshold) // Hvis døren er nesten lukket
        {
            Close(); // Lukk døren
        }
    }

    private void Close()
    {
        hinge.rotation = closedRotation; // Setter døren tilbake til startposisjon
    }
}
