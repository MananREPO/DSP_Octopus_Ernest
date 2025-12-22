using System.Collections;
using UnityEngine;

public class ItemCarrier : MonoBehaviour
{
    public Animator animator;

    [Header("Hold Settings")]
    [SerializeField] Transform holdPoint;
    [SerializeField] KeyCode dropKey = KeyCode.G;

    [Header("Drop / Throw")]
    [SerializeField] bool throwOnDrop = true;
    [SerializeField] float throwForceForward = 2.5f;
    [SerializeField] float throwForceUp = 1.0f;
    [SerializeField] float rePickupCooldown = 0.25f;

    [Header("Optional")]
    [SerializeField] Transform aimTransform;

    private ProximityCollect heldItem;
    private Rigidbody heldRb;
    private Collider[] heldColliders;
    private Collider[] playerColliders;

    void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider>(true);
    }

    void Update()
    {
        if (heldItem != null && Input.GetKeyDown(dropKey))
            Drop();
    }

    public bool CanPickup(ProximityCollect item) => heldItem == null;

    public void Pickup(ProximityCollect item)
    {
        if (item == null || heldItem != null || holdPoint == null) return;

        heldItem = item;

        heldRb = item.GetComponent<Rigidbody>();
        heldColliders = item.GetComponentsInChildren<Collider>(true);

        if (heldRb != null)
        {
            heldRb.useGravity = false;
            heldRb.isKinematic = true;
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
        }

        if (heldColliders != null)
        {
            foreach (var c in heldColliders)
                if (c != null) c.enabled = false;
        }

        item.transform.SetParent(holdPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        item.SetHeld(true);
    }

    public void Drop()
    {
        if (heldItem == null) return;

        var item = heldItem;
        var rb = heldRb;
        var cols = heldColliders;

        heldItem = null;
        heldRb = null;
        heldColliders = null;

        item.transform.SetParent(null);
        item.SetHeld(false);

        animator.SetTrigger("isThrow");
        StartCoroutine(ReenableCollidersAndPhysics(item, rb, cols));
    }

    IEnumerator ReenableCollidersAndPhysics(ProximityCollect item, Rigidbody rb, Collider[] cols)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (cols != null)
        {
            foreach (var c in cols)
                if (c != null) c.enabled = true;

            foreach (var itemCol in cols)
            {
                if (itemCol == null) continue;
                foreach (var pCol in playerColliders)
                {
                    if (pCol == null) continue;
                    Physics.IgnoreCollision(itemCol, pCol, true);
                }
            }
        }

        if (throwOnDrop && rb != null)
        {
            Transform aim = aimTransform != null ? aimTransform : (Camera.main != null ? Camera.main.transform : transform);
            Vector3 force = aim.forward * throwForceForward + Vector3.up * throwForceUp;
            rb.AddForce(force, ForceMode.Impulse);
        }

        yield return new WaitForSeconds(rePickupCooldown);

        if (cols != null)
        {
            foreach (var itemCol in cols)
            {
                if (itemCol == null) continue;
                foreach (var pCol in playerColliders)
                {
                    if (pCol == null) continue;
                    Physics.IgnoreCollision(itemCol, pCol, false);
                }
            }
        }
    }
}
