using UnityEngine;
using TMPro;
using System.Collections;

public class ProximityCollect : MonoBehaviour
{
    [Header("Settings")]
    public string itemName = "prop";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI uiText;

    public Animator animator;

    private bool isPlayerInRange = false;
    private bool isHeld = false;

    private ItemCarrier carrier;

    void Start()
    {
        if (uiText != null) uiText.text = "";
    }

    void Update()
    {
        if (isHeld) return;

        if (isPlayerInRange && Input.GetMouseButtonDown(0))
        {
            if (carrier != null && carrier.CanPickup(this))
            {
                //animator.SetTrigger("isGrab");
                animator.SetBool("isGrabbing", true);
                StartCoroutine(WaitForAnimation());

                if (uiText != null) uiText.text = "";
                carrier.Pickup(this);
            }

            else
            {
                if (uiText != null) uiText.text = "Hands full (press G to drop)";
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isHeld) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            carrier = other.GetComponent<ItemCarrier>();
            if (carrier == null) carrier = other.GetComponentInParent<ItemCarrier>();

            if (uiText != null)
                uiText.text = "Left click to grab " + itemName;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (uiText != null) uiText.text = "";
        }
    }

    public void SetHeld(bool held)
    {
        isHeld = held;
        isPlayerInRange = false;
        if (uiText != null) uiText.text = "";
    }

    IEnumerator WaitForAnimation()
    {
        yield return new WaitForSeconds(1.5f);
    }
}
