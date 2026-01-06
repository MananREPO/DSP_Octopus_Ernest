using UnityEngine;

public class SharkPath : MonoBehaviour
{
    public Transform[] Waypoints { get; private set; }

    private void Awake()
    {
        int count = transform.childCount;
        Waypoints = new Transform[count];
        for (int i = 0; i < count; i++)
            Waypoints[i] = transform.GetChild(i);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < transform.childCount; i++)
        {
            var a = transform.GetChild(i).position;
            var b = transform.GetChild((i + 1) % transform.childCount).position;
            Gizmos.DrawSphere(a, 0.3f);
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}
