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

    private bool isPaused = false;
    private bool isDashing = false;
    private float lastDashTime = -10f;
    private Rigidbody rb;
    private bool movementLocked;
    [SerializeField] private BubbleTrailEmitter dashBubbles;
    [Header("Audio Settings")]
    [SerializeField] private AudioClip dashSFX;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.linearDamping = drag;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;


        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (!isPaused)
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
                //animator.SetTrigger("isSwim");
                animator.SetBool("isSwimming", true);
                animator.SetBool("isIdleing", false);
            }

            if (Input.GetKeyUp(KeyCode.W))
            {
                //animator.SetTrigger("isIdle");
                animator.SetBool("isSwimming", false);
                animator.SetBool("isIdleing", true);

            }
        }

    }
    void FixedUpdate()
    {
        if (!isPaused)
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
    }

    IEnumerator PerformDash()
    {
        OctopusStats stats = GetComponent<OctopusStats>();
        if (stats != null && stats.UseStamina(25f))
        {
            //animator.SetTrigger("isDash");
            animator.SetBool("isDashing", true);
            animator.SetBool("isSwimming", false);
            if (dashSFX != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(dashSFX, 0.8f);
            }
            yield return new WaitForSeconds(0.75f);
            isDashing = true;
            lastDashTime = Time.time;
            if (dashBubbles != null) dashBubbles.SetEmitting(true);
            rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);

            yield return new WaitForSeconds(dashDuration);
            if (dashBubbles != null) dashBubbles.SetEmitting(false);
            isDashing = false;
            yield return new WaitForSeconds(0.2f);
            animator.SetBool("isDashing", false);
            animator.SetBool("isSwimming", true);
            //animator.SetTrigger("isSwim");
        }
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

    public void PauseGame()
    {
        isPaused = true;
    }
    public void UnPauseGame()
    {
        isPaused = false;
    }
}