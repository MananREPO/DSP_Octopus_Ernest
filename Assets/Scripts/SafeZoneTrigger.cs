using UnityEngine;

public class SafeZoneTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other, out var player)) return;

        var allSharks = Object.FindObjectsByType<SharkAI>(FindObjectsSortMode.None);
        foreach (var shark in allSharks)
            shark.OnEnterSafeZone(gameObject.GetInstanceID());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other, out var player)) return;

        var allSharks = Object.FindObjectsByType<SharkAI>(FindObjectsSortMode.None);
        SharkAI closestShark = null;
        float minDistance = float.MaxValue;

        foreach (var shark in allSharks)
        {
            shark.OnExitSafeZone(gameObject.GetInstanceID());


            if (!shark.IsInSafeZone())
            {
                float d = Vector3.Distance(shark.transform.position, player.position);
                if (d < minDistance) { minDistance = d; closestShark = shark; }
            }
        }
        closestShark?.BeginHunt(player);
    }
    private bool IsPlayer(Collider other, out Transform playerRoot)
    {
        playerRoot = other.transform.root;
        return playerRoot.CompareTag(playerTag);
    }
}