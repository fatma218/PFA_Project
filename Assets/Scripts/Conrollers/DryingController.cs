using UnityEngine;
using System.Collections;

public class DryingController : MonoBehaviour
{
    [Header("Lumière bleue fixe sur le tissu (rapport section 2.1.1)")]
    public Light tabLight;

    [Header("Tissu stérile")]
    [Tooltip("Le GameObject Paper Towel avec Grab Interaction Meta SDK")]
    public GameObject tissueObject;

    [Header("Détection séchage")]
    [Tooltip("Vitesse minimum (m/s) pour considérer que le tissu est utilisé activement")]
    public float minMovementVelocity = 0.1f;
    [Tooltip("Durée cumulée de mouvement requise pour valider le séchage")]
    public float minDryingDuration = 3f;

    [Header("Audio (optionnel)")]
    public AudioSource audioSource;
    public AudioClip soundTrashDrop;

    private bool isDryingActive = false;
    private bool dryingComplete = false;
    private float dryingTimer = 0f;
    private Vector3 lastTissuePos;

    void Start()
    {
        HandWashingManager.OnStepChanged += OnStepChanged;
        if (tabLight != null) tabLight.enabled = false;
        if (tissueObject != null) tissueObject.SetActive(false);
    }

    void OnDestroy()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
    }

    void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.DryingHands)
        {
            isDryingActive = true;
            dryingComplete = false;
            dryingTimer = 0f;

            if (tabLight != null) tabLight.enabled = true;
            if (tissueObject != null)
            {
                tissueObject.SetActive(true);
                lastTissuePos = tissueObject.transform.position;
            }

            Debug.Log("🧻 Phase Séchage — attrape le tissu (lumière bleue) et sèche tes mains");
        }
        else
        {
            isDryingActive = false;
            if (tabLight != null) tabLight.enabled = false;
            if (tissueObject != null) tissueObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isDryingActive || dryingComplete || tissueObject == null) return;

        // Détection : le tissu se déplace = il est tenu et utilisé pour sécher
        Vector3 currentPos = tissueObject.transform.position;
        float velocity = (currentPos - lastTissuePos).magnitude / Time.deltaTime;
        lastTissuePos = currentPos;

        if (velocity > minMovementVelocity)
        {
            dryingTimer += Time.deltaTime;
            if (dryingTimer >= minDryingDuration)
            {
                dryingComplete = true;
                Debug.Log("✅ Séchage suffisant — jette le tissu dans la poubelle");
            }
        }
    }

    // Appelée par le script TrashZone quand le tissu entre dans la poubelle
    public void OnTissueThrownInTrash(GameObject thrownTissue)
    {
        if (!isDryingActive) return;

        if (!dryingComplete)
        {
            Debug.LogWarning("⚠️ Tissu jeté trop tôt — il faut sécher plus longtemps !");
            return;
        }

        if (audioSource != null && soundTrashDrop != null)
            audioSource.PlayOneShot(soundTrashDrop);

        StartCoroutine(AnimateTissueIntoTrash(thrownTissue));

        Debug.Log("🗑️ Tissu jeté — Lavage complet réussi !");
        HandWashingManager.Instance?.CompleteDryingStep();
        isDryingActive = false;
    }

    private IEnumerator AnimateTissueIntoTrash(GameObject tissue)
    {
        if (tissue == null) yield break;

        Vector3 originalScale = tissue.transform.localScale;
        float duration = 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            tissue.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }
        tissue.SetActive(false);
    }
}