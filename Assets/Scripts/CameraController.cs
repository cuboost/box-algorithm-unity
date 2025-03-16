using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float rotationSpeed = 10f; // Speed of camera rotation
    public float returnSpeed = 2f; // Speed of returning to center
    private float yaw = 0f;
    private float pitch = 0f;
    private Quaternion initialRotation;

    void Start()
    {
        // Store the initial rotation of the camera
        initialRotation = transform.rotation;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        // Get the screen center
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Get the mouse position
        Vector2 mousePos = Input.mousePosition;

        // Calculate the offset from the center
        Vector2 offset = mousePos - screenCenter;

        // Normalize the offset
        offset /= screenCenter;

        // Calculate yaw and pitch based on the offset
        yaw += offset.x * rotationSpeed * Time.deltaTime;
        pitch -= offset.y * rotationSpeed * Time.deltaTime;

        // Clamp pitch to prevent flipping
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        // Smoothly interpolate the camera rotation to return to the initial rotation
        yaw = Mathf.Lerp(yaw, initialRotation.eulerAngles.y, returnSpeed * Time.deltaTime);
        pitch = Mathf.Lerp(pitch, initialRotation.eulerAngles.x, returnSpeed * Time.deltaTime);

        // Apply rotation to the camera
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
}