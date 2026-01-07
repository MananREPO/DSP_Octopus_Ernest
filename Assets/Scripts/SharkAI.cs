using System.Reflection;
using UnityEngine;

public class SharkAI : MonoBehaviour
{
    private enum State { Patrol, Hunt }

    [Header("References")]
    [SerializeField] private SharkPath path;
    [SerializeField] private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 6f;
    [SerializeField] private float huntSpeed = 12f;
    [SerializeField] private float turnSpeed = 6f;

    [Header("Hunt Vertical")]
    [SerializeField] private bool huntUseVertical = true;
    [SerializeField] private float maxVerticalSpeed = 8f;

    [Header("Model Axis Fix")]
    [SerializeField] private float modelYawOffset = 90f;

    [Header("Patrol")]
    [SerializeField] private float waypointReachDist = 2.5f;
    [SerializeField] private float minSegmentLength = 1.0f;

    [Header("Hunt Start Surge")]
    [SerializeField] private float huntRampSeconds = 1.0f;
    [SerializeField] private float huntSpeedMultiplierOnStart = 1.6f;
    [SerializeField] private float huntTurnBoost = 2.0f;
    [SerializeField] private float huntTurnBoostDuration = 0.35f;

    [Header("Ink Reaction")]
    [SerializeField] private float inkConfuseSeconds = 2.5f;
    [SerializeField] private float inkInvestigateSeconds = 3.5f;
    [SerializeField] private float inkHearingRadius = 25f;

    [Header("Kill On Touch")]
    [SerializeField] private bool killOnTouch = true;
    [SerializeField] private string playerTag = "Player";

    [SerializeField] private float inkDepthMin = 3f;
    [SerializeField] private float inkDepthMax = 5f;
    [SerializeField] private float inkHoldRadius = 0.75f;
    [SerializeField] private float inkMoveSpeed = 8f;

    private float inkLockUntil;
    private Vector3 inkLockPos;


    private State state = State.Patrol;

    private Transform targetPlayer;
    private int wpIndex;

    private float confusedUntil;
    private float investigateUntil;
    private Vector3 investigatePos;

    private float huntStartTime;
    private float baseHuntSpeed;
    private float baseTurnSpeed;

    private CamoOctopus camo;
    private static FieldInfo camoField;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (path == null) path = FindFirstObjectByType<SharkPath>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        baseTurnSpeed = turnSpeed;
        baseHuntSpeed = huntSpeed;

        wpIndex = GetClosestWaypointIndex();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        bool isCamo = GetCamoState();

        if (Time.time < inkLockUntil)
        {
            float d = Vector3.Distance(rb.position, inkLockPos);
            if (d > inkHoldRadius)
                MoveTowards(inkLockPos, inkMoveSpeed, true);

            return;
        }
        else if (inkLockUntil > 0f)
        {
            inkLockUntil = 0f;
            huntStartTime = Time.time;
        }

        if (targetPlayer != null && isCamo)
        {
            state = State.Patrol;
            confusedUntil = 0f;
        }
        else if (targetPlayer != null)
        {
            state = State.Hunt;
        }
        else
        {
            state = State.Patrol;
        }

        if (Time.time < confusedUntil)
        {
            if (Time.time < investigateUntil)
                MoveTowards(investigatePos, patrolSpeed, false);
            return;
        }

        if (state == State.Hunt) HuntMove();
        else PatrolMove();
    }


    public void BeginHunt(Transform player)
    {
        targetPlayer = player;
        camo = player != null ? player.GetComponentInChildren<CamoOctopus>() : null;

        huntStartTime = Time.time;

        turnSpeed = baseTurnSpeed * huntTurnBoost;
        CancelInvoke(nameof(ResetTurnSpeed));
        Invoke(nameof(ResetTurnSpeed), huntTurnBoostDuration);

        state = State.Hunt;
    }

    public void CancelHunt()
    {
        targetPlayer = null;
        camo = null;

        confusedUntil = 0f;
        investigateUntil = 0f;

        turnSpeed = baseTurnSpeed;
        CancelInvoke(nameof(ResetTurnSpeed));

        state = State.Patrol;
    }

    public void NotifyInk(Vector3 playerPos)
    {
        float depth = Random.Range(inkDepthMin, inkDepthMax);

        inkLockPos = playerPos + Vector3.down * depth;
        inkLockUntil = Time.time + inkInvestigateSeconds;

        confusedUntil = Time.time + inkConfuseSeconds;
        investigateUntil = inkLockUntil;
        investigatePos = inkLockPos;

        state = State.Patrol;
    }


    private void ResetTurnSpeed()
    {
        turnSpeed = baseTurnSpeed;
    }

    private void HuntMove()
    {
        if (targetPlayer == null) { state = State.Patrol; return; }
        if (GetCamoState()) { state = State.Patrol; return; }

        float t = Mathf.Clamp01((Time.time - huntStartTime) / Mathf.Max(0.0001f, huntRampSeconds));
        float currentHuntSpeed = Mathf.Lerp(baseHuntSpeed * huntSpeedMultiplierOnStart, baseHuntSpeed, t);

        MoveTowards(targetPlayer.position, currentHuntSpeed, huntUseVertical);
    }

    private void PatrolMove()
    {
        if (Time.time < investigateUntil)
        {
            MoveTowards(investigatePos, patrolSpeed, false);
            return;
        }

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

        MoveTowards(path.Waypoints[wpIndex].position, patrolSpeed, false);
    }

    private void MoveTowards(Vector3 targetPos, float speed, bool includeY)
    {
        if (rb == null) return;

        Vector3 pos = rb.position;

        Vector3 dirMove = targetPos - pos;
        if (!includeY) dirMove.y = 0f;

        if (dirMove.sqrMagnitude < 0.0001f) return;

        Vector3 dirLook = targetPos - pos;
        dirLook.y = 0f;
        if (dirLook.sqrMagnitude < 0.0001f) dirLook = new Vector3(dirMove.x, 0f, dirMove.z);

        dirMove.Normalize();
        dirLook.Normalize();

        Quaternion desiredRot = Quaternion.LookRotation(dirLook, Vector3.up) * Quaternion.Euler(0f, modelYawOffset, 0f);
        Quaternion newRot = Quaternion.Slerp(rb.rotation, desiredRot, turnSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);

        Vector3 delta = dirMove * (speed * Time.fixedDeltaTime);

        if (includeY)
            delta.y = Mathf.Clamp(delta.y, -maxVerticalSpeed * Time.fixedDeltaTime, maxVerticalSpeed * Time.fixedDeltaTime);

        rb.MovePosition(pos + delta);
    }

    private bool GetCamoState()
    {
        if (camo == null) return false;

        if (camoField == null)
            camoField = typeof(CamoOctopus).GetField("isCamouflaged", BindingFlags.Instance | BindingFlags.NonPublic);

        if (camoField == null) return false;

        object v = camoField.GetValue(camo);
        return v is bool b && b;
    }

    private int GetClosestWaypointIndex()
    {
        if (path == null || path.Waypoints == null || path.Waypoints.Length == 0) return 0;

        Vector3 pos = rb != null ? rb.position : transform.position;

        int best = 0;
        float bestD = float.MaxValue;

        for (int i = 0; i < path.Waypoints.Length; i++)
        {
            float d = FlatDistance(pos, path.Waypoints[i].position);
            if (d < bestD) { bestD = d; best = i; }
        }

        return best;
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!killOnTouch) return;
        if (state != State.Hunt) return;

        Transform root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        Destroy(root.gameObject);
    }
}
