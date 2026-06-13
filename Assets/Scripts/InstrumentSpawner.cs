using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

/// <summary>
/// Spawne un instrument grabbable avec tous les composants Meta necessaires.
/// Branche sur Button.OnClick via SlotButtonBinder.
/// </summary>
public class InstrumentSpawner : MonoBehaviour
{
    [Header("Instrument a spawner")]
    public GameObject instrumentPrefab;

    [Header("Reglages spawn")]
    public float spawnDistance     = 0.5f;
    public float spawnHeightOffset = -0.1f;

    private GameObject spawnedInstrument;

    public void SpawnInstrument()
    {
        // Detruire l'ancien si existe
        if (spawnedInstrument != null)
            Destroy(spawnedInstrument);

        if (instrumentPrefab == null)
        {
            Debug.LogError("[Spawner] instrumentPrefab non assigne sur " + gameObject.name);
            return;
        }

        // Position devant la tete du joueur
        Transform head = Camera.main != null ? Camera.main.transform : transform;
        Vector3 spawnPos = head.position
                         + head.forward * spawnDistance
                         + Vector3.up * spawnHeightOffset;

        // Instancier le prefab
        spawnedInstrument = Instantiate(instrumentPrefab, spawnPos, head.rotation);
        spawnedInstrument.name = "Spawned_" + instrumentPrefab.name;

        // Ajouter Rigidbody
        Rigidbody rb = spawnedInstrument.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spawnedInstrument.AddComponent<Rigidbody>();
            rb.mass        = 0.1f;
            rb.drag        = 1f;
            rb.angularDrag = 1f;
        }

        // Ajouter BoxCollider si aucun collider
        if (spawnedInstrument.GetComponentInChildren<Collider>() == null)
        {
            var col = spawnedInstrument.AddComponent<BoxCollider>();
            col.size = new Vector3(0.05f, 0.02f, 0.15f);
        }

        // Ajouter Grabbable Meta (base obligatoire)
        Grabbable grabbable = spawnedInstrument.GetComponent<Grabbable>();
        if (grabbable == null)
            grabbable = spawnedInstrument.AddComponent<Grabbable>();

        // Ajouter GrabInteractable
        GrabInteractable grabInteractable = spawnedInstrument.GetComponent<GrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = spawnedInstrument.AddComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rb);
        }

        // Ajouter HandGrabInteractable pour le hand tracking precis
        HandGrabInteractable handGrab = spawnedInstrument.GetComponent<HandGrabInteractable>();
        if (handGrab == null)
        {
            handGrab = spawnedInstrument.AddComponent<HandGrabInteractable>();
            handGrab.InjectRigidbody(rb);
        }

        Debug.Log("[Spawner] " + spawnedInstrument.name + " spawne et grabable a " + spawnPos);
    }

    public void DespawnInstrument()
    {
        if (spawnedInstrument != null)
        {
            Destroy(spawnedInstrument);
            spawnedInstrument = null;
        }
    }
}
