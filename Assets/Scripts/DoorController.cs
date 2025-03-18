using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform doorHinge; // Hinge (DoorHinge) transform
    private bool isGrabbed = false; // Sjekker om spilleren kontrollerer døren
    public float closeThreshold = 20f; // Grense for automatisk lukking
    public float rotationSpeed = 5f; // Hvor raskt døren roteres med musa
    public Transform doorCubeMesh; // Referanse til selve dør-meshen (DoorCubeMesh)

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isGrabbed = !isGrabbed; // Toggle kontroll
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
        Vector3 newRotation = doorHinge.localEulerAngles; // Bruker Hinge-pivot for rotasjon
        newRotation.y += mouseX * rotationSpeed; // Roterer døren på Y-aksen
        doorHinge.localEulerAngles = newRotation;
    }

    private void CheckClose()
    {
        float angle = Mathf.Abs(Mathf.DeltaAngle(doorCubeMesh.localEulerAngles.y, 0)); // Beregner reell vinkel til lukket posisjon (0)
        if (angle < closeThreshold) // Hvis døren er nesten lukket
        {
            Close(); // Lukk døren
        }
    }

    private void Close()
    {
        doorCubeMesh.localEulerAngles = new Vector3(0, 0, 0); // Sett døren tilbake til lukket posisjon
    }
}
