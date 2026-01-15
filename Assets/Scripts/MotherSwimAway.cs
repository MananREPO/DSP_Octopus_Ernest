using System.Collections;
using UnityEngine;

public class MotherSwimAway : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public float speed = 3f;
    public float rotateSpeed = 6f;
    public float reachDist = 0.2f;

    public Animator animator;
    public string swimTrigger = "isSwim";

    public float yawOffset = 180f;

    private bool started;

    public void StartSwimAway()
    {
        if (started) return;
        started = true;

        if (animator != null && !string.IsNullOrEmpty(swimTrigger))
            animator.SetTrigger(swimTrigger);

        StopAllCoroutines();
        StartCoroutine(SwimRoutine());
    }

    private IEnumerator SwimRoutine()
    {
        if (pointA != null) yield return MoveTo(pointA.position);
        if (pointB != null) yield return MoveTo(pointB.position);

        gameObject.SetActive(false);
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > reachDist)
        {
            Vector3 dir = (target - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, yawOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }
}