using System.Collections;
using System.Reflection;
using UnityEngine;

public class SharkAI : MonoBehaviour
{
    private enum State { Patrol, Hunt, InkAttack, Eating }

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
    [SerializeField] private float inkAttackSeconds = 3.5f;
    [SerializeField] private float inkAttackReachDist = 1.2f;
    [SerializeField] private float inkConfusedTime = 5f;
    private float inkConfusedUntil;
    private Vector3 inkConfusedPosition;
    private Transform rememberedPlayer;

    [Header("Kill On Touch")]
    [SerializeField] private bool killOnTouch = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Animator animator;


    private State state = State.Patrol;
    private System.Collections.Generic.HashSet<int> activeSafeZones = new System.Collections.Generic.HashSet<int>();
    private Transform targetPlayer;
    private int wpIndex;

    private float huntStartTime;
    private float baseHuntSpeed;
    private float baseTurnSpeed;

    private CamoOctopus camo;
    private static FieldInfo camoField;

    private Transform inkTarget;
    private float inkTargetUntil;

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
        if (rb == null || state == State.Eating) return;

        if (Time.time < inkConfusedUntil)
        {
            MoveTowards(inkConfusedPosition, patrolSpeed, false);
            return;
        }

        if (inkConfusedUntil > 0f && Time.time >= inkConfusedUntil)
        {
            inkConfusedUntil = 0f;

            if (activeSafeZones.Count == 0 && rememberedPlayer != null)
            {
                BeginHunt(rememberedPlayer);
            }
        }

        if (inkTarget != null && Time.time < inkTargetUntil)
        {
            state = State.InkAttack;

            float d = Vector3.Distance(rb.position, inkTarget.position);

            if (d > inkAttackReachDist)
            {
                MoveTowards(inkTarget.position, huntSpeed, huntUseVertical);
            }
            else
            {
                inkConfusedPosition = inkTarget.position;
                inkConfusedUntil = Time.time + inkConfusedTime;

                inkTarget = null;
                inkTargetUntil = 0f;

                if (animator != null) animator.SetBool("isFast", false);

                state = State.Patrol;
            }

            return;
        }

        if (state == State.InkAttack)
        {
            inkTarget = null;
            inkTargetUntil = 0f;

            inkConfusedPosition = rb.position;
            inkConfusedUntil = Time.time + inkConfusedTime;

            if (animator != null) animator.SetBool("isFast", false);

            state = State.Patrol;
        }

        if (targetPlayer != null)
        {
            var camoComp = targetPlayer.GetComponentInChildren<CamoOctopus>();
            if (camoComp != null && camoComp.IsCamouflaged)
            {
                CancelHunt();
            }
        }

        if (targetPlayer != null) state = State.Hunt;
        else state = State.Patrol;

        if (state == State.Hunt) HuntMove();
        else PatrolMove();
    }

    public void BeginHunt(Transform player)
    {
        if (activeSafeZones.Count > 0) return;
        if (inkTarget != null && Time.time < inkTargetUntil) return;

        animator.SetBool("isFast", true);
        targetPlayer = player;

        var camoComp = player != null ? player.GetComponentInChildren<CamoOctopus>() : null;
        if (camoComp != null && camoComp.IsCamouflaged)
        {
            CancelHunt();
            return;
        }

        rememberedPlayer = player;
        camo = player != null ? player.GetComponentInChildren<CamoOctopus>() : null;
        if (camo != null)
        {
            camo.OnCamoChanged -= HandleCamoChanged;
            camo.OnCamoChanged += HandleCamoChanged;
        }

        huntStartTime = Time.time;

        turnSpeed = baseTurnSpeed * huntTurnBoost;
        CancelInvoke(nameof(ResetTurnSpeed));
        Invoke(nameof(ResetTurnSpeed), huntTurnBoostDuration);
        AudioManager.Instance?.SetChaseState(true);
        state = State.Hunt;
    }

    public void CancelHunt(bool unsubscribeCamo = true)
    {
        targetPlayer = null;

        if (unsubscribeCamo && camo != null)
        {
            camo.OnCamoChanged -= HandleCamoChanged;
            camo = null;
        }

        if (animator != null) animator.SetBool("isFast", false);
        turnSpeed = baseTurnSpeed;
        CancelInvoke(nameof(ResetTurnSpeed));
        AudioManager.Instance?.SetChaseState(false);
        state = State.Patrol;
    }

    public void NotifyInk(Transform inkCloud)
    {
        if (inkCloud == null) return;

        if (state != State.Hunt || targetPlayer == null) return;

        rememberedPlayer = targetPlayer;

        targetPlayer = null;
        camo = null;

        inkTarget = inkCloud;
        inkTargetUntil = Time.time + inkAttackSeconds;

        state = State.InkAttack;
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
        if (path == null || path.Waypoints == null || path.Waypoints.Length < 2) return;

        Vector3 sharkPos = rb.position;
        Vector3 wpPos = path.Waypoints[wpIndex].position;

        sharkPos.y = 0f;
        wpPos.y = 0f;

        float horizontalDist = Vector3.Distance(sharkPos, wpPos);

        if (horizontalDist <= waypointReachDist)
        {
            wpIndex = (wpIndex + 1) % path.Waypoints.Length;
        }

        MoveTowards(path.Waypoints[wpIndex].position, patrolSpeed, false);
    }

    private void MoveTowards(Vector3 targetPos, float speed, bool includeY)
    {
        if (rb == null) return;

        Vector3 pos = rb.position;
        Vector3 dirMove = targetPos - pos;
        if (!includeY) dirMove.y = 0f;

        if (dirMove.magnitude < 0.5f) return;

        Vector3 dirLook = targetPos - pos;
        dirLook.y = 0f;

        if (dirLook.magnitude < 0.5f) dirLook = transform.forward;

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
            float d = Vector3.Distance(pos, path.Waypoints[i].position);
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
        if (!killOnTouch || state != State.Hunt) return;

        Transform root = other.transform.root;
        if (!root.CompareTag(playerTag)) return;

        state = State.Eating;


        if (animator != null)
        {
            animator.SetTrigger("Eat");
        }


        root.gameObject.SetActive(false);


        StartCoroutine(FinishEating(root.gameObject));
    }

    private IEnumerator FinishEating(GameObject playerObj)
    {
        yield return new WaitForSeconds(2.0f);
        AudioManager.Instance?.SetChaseState(false);
        if (playerObj != null) Destroy(playerObj);

        targetPlayer = null;
        camo = null;
        state = State.Patrol;

        if (animator != null) animator.SetBool("isFast", false);

        wpIndex = GetClosestWaypointIndex();
    }

    private void HandleCamoChanged(bool camoOn)
    {
        if (camoOn)
        {
            CancelHunt(unsubscribeCamo: false);
            return;
        }

        if (activeSafeZones.Count == 0 && rememberedPlayer != null)
        {
            BeginHunt(rememberedPlayer);
        }
    }

    public void OnEnterSafeZone(int zoneID)
    {
        activeSafeZones.Add(zoneID);
        CancelHunt(unsubscribeCamo: true);
    }

    public void OnExitSafeZone(int zoneID) { activeSafeZones.Remove(zoneID); }

    public bool IsInSafeZone() => activeSafeZones.Count > 0;
}