using UnityEngine;

/// <summary>
/// Attache ce script sur la zone de teleportation devant le lavabo.
/// Bloque le son de guidage au demarrage.
/// Le reactive uniquement quand le joueur entre dans la zone.
/// </summary>
public class SinkTeleportTrigger : MonoBehaviour
{
    [Header("Reference au HandWashingManager")]
    public HandWashingManager handWashingManager;

    [Header("Visuel de la zone (cercle bleu)")]
    public GameObject zoneVisual;

    [Header("Rayon de detection en metres")]
    public float teleportRadius = 1.2f;

    private bool playerArrived = false;
    private Transform playerHead;

    void Start()
    {
        // Bloquer autoStartOnPlay au demarrage
        if (handWashingManager != null)
        {
            handWashingManager.autoStartOnPlay = false;

            // Stopper l'audio au cas ou il aurait deja joue
            AudioSource audio = handWashingManager.GetComponent<AudioSource>();
            if (audio != null) audio.Stop();
        }

        // Afficher le cercle bleu
        if (zoneVisual != null)
            zoneVisual.SetActive(true);

        // Recuperer la camera principale (tete du joueur)
        if (Camera.main != null)
            playerHead = Camera.main.transform;
    }

    void Update()
    {
        if (playerArrived || playerHead == null) return;

        // Distance horizontale uniquement (ignore Y)
        float dist = Vector2.Distance(
            new Vector2(playerHead.position.x, playerHead.position.z),
            new Vector2(transform.position.x, transform.position.z)
        );

        if (dist <= teleportRadius)
            OnPlayerArrivedAtSink();
    }

    void OnPlayerArrivedAtSink()
    {
        playerArrived = true;

        // Cacher le visuel de la zone
        if (zoneVisual != null)
            zoneVisual.SetActive(false);

        // Lancer le lavage (et donc le son voiceWetHands)
        if (handWashingManager != null)
            handWashingManager.StartWashing();

        Debug.Log("[SinkTeleportTrigger] Joueur arrive au lavabo - demarrage du lavage et du son.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, teleportRadius);
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, teleportRadius);
    }
}
