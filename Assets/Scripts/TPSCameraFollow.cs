using UnityEngine;

public class TPSCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new (0, 2.5f, -4f);

    [Header("Settings")]
    public float mouseSensitivity = 3f;
    public float followSpeed = 20f;

    private float rotY = 0f;
    private float rotX = 0f; 
    void Start()
    {
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float inputX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float inputY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotY += inputX;
        rotX -= inputY;
        rotX = Mathf.Clamp(rotX, -20f, 60f);

        Quaternion rotation = Quaternion.Euler(rotX, rotY, 0);

        Vector3 desiredPosition = target.position + (rotation * offset);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        transform.LookAt(target.position + Vector3.up * 1f);
    }
}