using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Rigidbody))]
public class OctopusController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 50f;
    public float maxSpeed = 10f;
    public float drag = 3f;

    [Header("Dash Settings")]
    public float dashForce = 30f;       
    public float dashDuration = 0.5f;   
    public float dashCooldown = 1.5f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    private bool isDashing = false;
    private float lastDashTime = -10f;
    private Rigidbody rb;
    private bool movementLocked;
    [SerializeField] private BubbleTrailEmitter dashBubbles;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.linearDamping = drag;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;


        rb.constraints = RigidbodyConstraints.FreezeRotationZ;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        Vector3 camEuler = cameraTransform.eulerAngles;

        transform.rotation = Quaternion.Euler(camEuler.x, camEuler.y, 0f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time >= lastDashTime + dashCooldown)
            {
                StartCoroutine(PerformDash());
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            animator.SetTrigger("isSwim");
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            animator.SetTrigger("isIdle");
        }
    }
    void FixedUpdate()
    {
        if (movementLocked) return;

        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(v) > 0.1f)
        {
            Vector3 moveDir = cameraTransform.forward;
            moveDir.Normalize();
            
            rb.AddForce(moveDir * v * moveSpeed, ForceMode.Acceleration);
        }

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    IEnumerator PerformDash()
    {
        animator.SetTrigger("isDash");
        yield return new WaitForSeconds(1.3f);
        isDashing = true;
        lastDashTime = Time.time;
        if (dashBubbles != null) dashBubbles.SetEmitting(true);
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);

        yield return new WaitForSeconds(dashDuration);
        if (dashBubbles != null) dashBubbles.SetEmitting(false);
        isDashing = false;
        yield return new WaitForSeconds(0.2f);
        animator.SetTrigger("isSwim");
    }

    void OnCollisionEnter(Collision collision)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        movementLocked = true;
        StartCoroutine(UnlockMovementAfterDelay(0.05f));
    }

    IEnumerator UnlockMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        movementLocked = false;
    }
}