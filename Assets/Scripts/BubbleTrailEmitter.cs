using UnityEngine;

public class BubbleTrailEmitter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] ParticleSystem bubblePS;
    [SerializeField] Transform emitPoint;

    [Header("Speed Detection")]
    [SerializeField] Rigidbody rb;
    [SerializeField] float minSpeed = 0.15f;

    [Header("Emission")]
    [SerializeField] float bubblesPerSecondAtSpeed1 = 18f;
    [SerializeField] float maxBubblesPerSecond = 80f;
    [SerializeField] float maxSpeedForScaling = 6f;
    [SerializeField] float spawnRadius = 0.08f;
    [SerializeField] float sideVelocity = 0.15f;
    [SerializeField] float upVelocity = 0.4f;
    [SerializeField] bool emitOnlyWhenEnabled = true;

    [Header("Direction")]
    [SerializeField] bool orientOppositeVelocity = true;

    Vector3 lastPos;
    float accumulator;
    private bool emitting;
    public void SetEmitting(bool value)
    {
        emitting = value;
    }
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (bubblePS == null) bubblePS = GetComponentInChildren<ParticleSystem>(true);
        if (emitPoint == null) emitPoint = bubblePS != null ? bubblePS.transform : transform;

        lastPos = transform.position;
    }

    void Update()
    {
        if (emitOnlyWhenEnabled && !emitting) return;
        if (bubblePS == null) return;

        float speed = GetSpeed();
        if (speed < minSpeed)
        {
            accumulator = 0f;
            lastPos = transform.position;
            return;
        }

        if (orientOppositeVelocity)
        {
            Vector3 v = GetVelocityVector();
            if (v.sqrMagnitude > 0.0001f)
                bubblePS.transform.forward = -v.normalized;
        }

        float scaledSpeed = Mathf.Clamp(speed, 0f, maxSpeedForScaling);
        float rate = bubblesPerSecondAtSpeed1 * scaledSpeed;
        rate = Mathf.Min(rate, maxBubblesPerSecond);

        accumulator += rate * Time.deltaTime;
        int emitCount = Mathf.FloorToInt(accumulator);

        if (emitCount > 0)
        {
            accumulator -= emitCount;

            var emitParams = new ParticleSystem.EmitParams
            {
                position = emitPoint.position + Random.insideUnitSphere * spawnRadius,
                velocity = new Vector3(
                Random.Range(-sideVelocity, sideVelocity),
                Random.Range(0.1f, upVelocity),
                Random.Range(-sideVelocity, sideVelocity)
    )
            };

            bubblePS.Emit(emitParams, emitCount);
        }

        lastPos = transform.position;
    }

    float GetSpeed()
    {
        if (rb != null) return rb.linearVelocity.magnitude;

        Vector3 delta = transform.position - lastPos;
        return delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
    }

    Vector3 GetVelocityVector()
    {
        if (rb != null) return rb.linearVelocity;

        Vector3 delta = transform.position - lastPos;
        return delta / Mathf.Max(Time.deltaTime, 0.0001f);
    }
}
