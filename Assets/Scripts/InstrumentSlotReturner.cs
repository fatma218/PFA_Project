using UnityEngine;
using Oculus.Interaction;
using System.Collections;

/// <summary>
/// Gere le retour automatique de l'instrument a son slot.
/// - Quand grabbed : kinematic OFF, gravite OFF
/// - Quand relache : detecte si proche du slot → snap
///                   sinon → retour progressif au slot
/// - Quand main s'approche du slot : highlight bleu
/// </summary>
public class InstrumentSlotReturner : MonoBehaviour
{
    [HideInInspector] public Vector3 slotPosition;
    [HideInInspector] public Quaternion slotRotation;
    [HideInInspector] public float snapDistance = 0.15f;
    [HideInInspector] public float returnSpeed  = 5f;
    [HideInInspector] public GameObject socketVisual;
    [HideInInspector] public Grabbable grabbable;
    [HideInInspector] public Rigidbody rb;

    private bool isGrabbed    = false;
    private bool isReturning  = false;
    private Material socketMat;

    // Couleurs socket
    private Color colorIdle     = new Color(0.2f, 0.6f, 1f, 0.3f);
    private Color colorHighlight = new Color(0.2f, 1f, 0.4f, 0.7f);
    private Color colorEmpty    = new Color(1f, 0.3f, 0.2f, 0.4f);

    void Start()
    {
        if (socketVisual != null)
            socketMat = socketVisual.GetComponent<Renderer>().material;

        // Ecouter les events grab
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnPointerEvent;
        }

        // Placer au slot au demarrage
        SnapToSlot();
    }

    void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                OnGrabbed();
                break;
            case PointerEventType.Unselect:
                OnReleased();
                break;
        }
    }

    void OnGrabbed()
    {
        isGrabbed   = true;
        isReturning = false;

        rb.isKinematic = false;
        rb.useGravity  = false;

        // Socket devient rouge (vide)
        if (socketMat != null)
            socketMat.color = colorEmpty;

        Debug.Log("[SlotReturner] " + gameObject.name + " grab");
    }

    void OnReleased()
    {
        isGrabbed = false;
        rb.useGravity = false;

        float dist = Vector3.Distance(transform.position, slotPosition);

        if (dist <= snapDistance)
        {
            // Snap immediat
            SnapToSlot();
        }
        else
        {
            // Retour progressif
            isReturning = true;
            StartCoroutine(ReturnToSlot());
        }
    }

    IEnumerator ReturnToSlot()
    {
        rb.isKinematic = true;

        while (isReturning)
        {
            transform.position = Vector3.Lerp(
                transform.position, slotPosition, returnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, slotRotation, returnSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, slotPosition) < 0.005f)
            {
                SnapToSlot();
                yield break;
            }
            yield return null;
        }
    }

    void SnapToSlot()
    {
        isReturning = false;
        isGrabbed   = false;

        rb.isKinematic  = true;
        rb.useGravity   = false;
        rb.velocity     = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = slotPosition;
        transform.rotation = slotRotation;

        // Socket redevient bleu (occupe)
        if (socketMat != null)
            socketMat.color = colorIdle;
    }

    void Update()
    {
        if (!isGrabbed && socketVisual != null)
        {
            // Highlight quand la main s'approche du slot
            // (detecte via distance entre instrument et slot)
            float dist = Vector3.Distance(transform.position, slotPosition);
            if (dist > 0.01f && dist < snapDistance * 1.5f)
            {
                if (socketMat != null)
                    socketMat.color = colorHighlight;
            }
        }
    }

    void OnDestroy()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }
}
