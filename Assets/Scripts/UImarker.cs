using UnityEngine;

public class UImarker : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("Spin")]
    public float spinSpeed = 120f;
    public Vector3 spinAxis = Vector3.up;

    void LateUpdate()
    {
        transform.localPosition = offset;

        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }
}