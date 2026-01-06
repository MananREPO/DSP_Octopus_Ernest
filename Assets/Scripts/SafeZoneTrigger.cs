using UnityEngine;

public class SafeZoneTrigger : MonoBehaviour
{
    [SerializeField] private SharkAI shark;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (shark == null)
            shark = FindFirstObjectByType<SharkAI>();
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private bool IsPlayer(Collider other, out Transform playerRoot)
    {
        playerRoot = other.transform.root;
        return playerRoot.CompareTag(playerTag);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other, out var player)) return;

        Debug.Log($"[SafeZone] Player EXIT: {player.name}", this);
        shark?.BeginHunt(player);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other, out var player)) return;

        Debug.Log($"[SafeZone] Player ENTER: {player.name}", this);
        shark?.CancelHunt();
    }
}
