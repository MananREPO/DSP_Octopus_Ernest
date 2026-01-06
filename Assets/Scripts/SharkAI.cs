using System.Collections;
using UnityEngine;

public class SharkAI : MonoBehaviour
{
    private enum State { Patrol, Hunt, Cooldown }

    [Header("References")]
    [SerializeField] private SharkPath path;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 6f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float waypointReachDist = 2f;

    [Header("Hunt")]
    [SerializeField] private float huntSpeed = 12f;
    [SerializeField] private float biteDistance = 2.0f;
    [SerializeField] private float biteCooldownSeconds = 2.0f;

    [SerializeField] private Transform huntSpawnPoint;

    [Header("Hunt Spawn Behind Player")]
    [SerializeField] private bool spawnBehindPlayer = true;
    [SerializeField] private float behindDistance = 14f;
    [SerializeField] private float sideOffset = 6f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private int spawnAttempts = 10;

    [SerializeField] private float spawnCheckRadius = 1.5f;
    [SerializeField] private LayerMask spawnBlockers;
    [SerializeField] private float modelYawOffset = 90f;
    [SerializeField] private float minSegmentLength = 1.0f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool killOnTouch = true;

    private Transform targetPlayer;
    private int wpIndex;
    private State state = State.Patrol;
    private Coroutine huntRoutine;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (path == null) path = FindFirstObjectByType<SharkPath>();
    }
    private void Start()
    {
        wpIndex = GetClosestWaypointIndex();
    }


    private void FixedUpdate()
    {
        if (state == State.Patrol)
            PatrolMove();
        else if (state == State.Hunt)
            HuntMove();
    }

    public void BeginHunt(Transform player)
    {
        targetPlayer = player;

        if (spawnBehindPlayer)
        {
            TrySpawnBehindPlayer(player);
        }
        else if (huntSpawnPoint != null)
        {
            rb.position = huntSpawnPoint.position;
            rb.rotation = huntSpawnPoint.rotation;
        }

        state = State.Hunt;

        if (huntRoutine != null) StopCoroutine(huntRoutine);
        huntRoutine = StartCoroutine(BiteCheckLoop());

        animator?.SetBool("IsHunting", true);
    }


    public void CancelHunt()
    {
        targetPlayer = null;
        animator?.SetBool("IsHunting", false);

        if (huntRoutine != null)
        {
            StopCoroutine(huntRoutine);
            huntRoutine = null;
        }

        state = State.Patrol;
    }
    private void TrySpawnBehindPlayer(Transform player)
    {
        Vector3 playerPos = player.position;


        Vector3 back = -player.forward;
        back.y = 0f;
        if (back.sqrMagnitude < 0.0001f) back = -player.up;
        back.Normalize();

        Vector3 right = player.right;
        right.y = 0f;
        right.Normalize();


        for (int i = 0; i < spawnAttempts; i++)
        {
            float side = Random.Range(-sideOffset, sideOffset);
            float dist = behindDistance + Random.Range(-2f, 2f);

            Vector3 candidate = playerPos + back * dist + right * side;
            candidate.y = playerPos.y + yOffset;


            bool blocked = Physics.CheckSphere(candidate, spawnCheckRadius, spawnBlockers, QueryTriggerInteraction.Ignore);
            if (blocked) continue;


            rb.position = candidate;

            Vector3 lookDir = (playerPos - candidate);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                rb.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

            return;
        }


    }




    private void PatrolMove()
    {
        if (path == null || path.Waypoints == null || path.Waypoints.Length < 2) return;

        int len = path.Waypoints.Length;

        if (FlatDistance(rb.position, path.Waypoints[wpIndex].position) <= waypointReachDist)
            wpIndex = (wpIndex + 1) % len;

        int guard = 0;
        while (guard < len)
        {
            int next = (wpIndex + 1) % len;
            float seg = FlatDistance(path.Waypoints[wpIndex].position, path.Waypoints[next].position);
            if (seg >= minSegmentLength) break;

            wpIndex = next;
            guard++;
        }

        MoveTowards(path.Waypoints[wpIndex].position, patrolSpeed);
    }



    private void HuntMove()
    {
        if (targetPlayer == null)
        {
            state = State.Patrol;
            return;
        }

        MoveTowards(targetPlayer.position, huntSpeed);
    }

    private void MoveTowards(Vector3 targetPos, float speed)
    {
        Vector3 dir = (targetPos - rb.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();

        Quaternion desiredRot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, modelYawOffset, 0f);
        Quaternion newRot = Quaternion.Slerp(rb.rotation, desiredRot, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);

        rb.MoveRotation(newRot);

        Vector3 newPos = rb.position + dir * (speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }


    private IEnumerator BiteCheckLoop()
    {
        while (state == State.Hunt)
        {
            if (targetPlayer != null)
            {
                float d = Vector3.Distance(rb.position, targetPlayer.position);
                if (d <= biteDistance)
                {
                    BitePlayer(targetPlayer);
                    state = State.Cooldown;
                    animator?.SetTrigger("Bite");

                    yield return new WaitForSeconds(biteCooldownSeconds);


                    if (targetPlayer != null) state = State.Hunt;
                    else state = State.Patrol;
                }
            }
            yield return null;
        }
    }

    private void BitePlayer(Transform player)
    {
        Destroy(player.gameObject);
    }
    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private int GetClosestWaypointIndex()
    {
        if (path == null || path.Waypoints == null || path.Waypoints.Length == 0) return 0;

        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            float d = FlatDistance(rb.position, path.Waypoints[i].position);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!killOnTouch) return;

        var root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        if (state != State.Hunt) return;

        Destroy(root.gameObject);
    }


}

public interface IKillable
{
    void Kill(string reason);
}
