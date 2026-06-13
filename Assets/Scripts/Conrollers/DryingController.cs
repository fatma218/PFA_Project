using UnityEngine;
using System.Collections;

/// <summary>
/// Gere l'etape de sechage des mains.
/// Le joueur attrape le tissu, le bouge pour secher, puis le jette dans la poubelle.
/// Pas d'animation — juste la logique grab + poubelle.
/// </summary>
public class DryingController : MonoBehaviour
{
    [Header("Tissu")]
    public GameObject tissueObject;

    [Header("Detection sechage")]
    public float minMovementVelocity = 0.1f;
    public float minDryingDuration   = 3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip soundTrashDrop;

    [Header("Lumiere / Glow")]
    public Light tabLight;
    public Renderer tabGlowRenderer;
    public float glowPulseSpeed    = 2f;
    public float glowMinIntensity  = 0.3f;
    public float glowMaxIntensity  = 2f;

    private bool isDryingActive  = false;
    private bool dryingComplete  = false;
    private float dryingTimer    = 0f;
    private Vector3 lastTissuePos;
    private Material glowMat;
    private bool glowActive = false;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Color baseGlowColor = new Color(0.2f, 0.8f, 1f);
    private Vector3 tissueInitialScale;

    void Start()
    {
        HandWashingManager.OnStepChanged  += OnStepChanged;
        HandWashingManager.OnContamination += OnContamination;

        if (tabLight != null) tabLight.enabled = false;
        if (tissueObject != null)
        {
            tissueInitialScale = tissueObject.transform.localScale;
            tissueObject.SetActive(false);
        }

        if (tabGlowRenderer != null)
        {
            glowMat = tabGlowRenderer.material;
            glowMat.EnableKeyword("_EMISSION");
            SetGlow(0f);
        }
    }

    void OnDestroy()
    {
        HandWashingManager.OnStepChanged  -= OnStepChanged;
        HandWashingManager.OnContamination -= OnContamination;
    }

    void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.DryingHands)
        {
            isDryingActive = true;
            dryingComplete = false;
            dryingTimer    = 0f;

            if (tabLight != null) tabLight.enabled = true;
            glowActive = true;

            if (tissueObject != null)
            {
                tissueObject.transform.localScale = tissueInitialScale;
                tissueObject.SetActive(true);
                lastTissuePos = tissueObject.transform.position;
            }

            Debug.Log("[Drying] Etape sechage — attrape le tissu et seche tes mains.");
        }
        else
        {
            isDryingActive = false;
            glowActive     = false;

            if (tabLight != null) tabLight.enabled = false;
            if (glowMat != null) SetGlow(0f);
            if (tissueObject != null) tissueObject.SetActive(false);
        }
    }

    void OnContamination()
    {
        isDryingActive = false;
        dryingComplete = false;
        dryingTimer    = 0f;
        glowActive     = false;

        if (tabLight != null) tabLight.enabled = false;
        if (glowMat != null) SetGlow(0f);
        if (tissueObject != null) tissueObject.SetActive(false);
    }

    void Update()
    {
        // Glow pulse
        if (glowActive && glowMat != null)
        {
            float t = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) / 2f;
            SetGlow(Mathf.Lerp(glowMinIntensity, glowMaxIntensity, t));
        }

        if (!isDryingActive || dryingComplete || tissueObject == null) return;

        // Detection mouvement tissu
        Vector3 currentPos = tissueObject.transform.position;
        float velocity = (currentPos - lastTissuePos).magnitude / Time.deltaTime;
        lastTissuePos = currentPos;

        if (velocity > minMovementVelocity)
        {
            dryingTimer += Time.deltaTime;
            if (dryingTimer >= minDryingDuration)
            {
                dryingComplete = true;
                Debug.Log("[Drying] Sechage OK — jette le tissu dans la poubelle.");
            }
        }
    }

    /// <summary>
    /// Appele par le script de poubelle quand le tissu est jete dedans.
    /// </summary>
    public void OnTissueThrownInTrash(GameObject thrownTissue)
    {
        if (!isDryingActive) return;

        if (!dryingComplete)
        {
            Debug.LogWarning("[Drying] Tissu jete trop tot — continue a secher tes mains.");
            return;
        }

        StartCoroutine(CompleteTrash(thrownTissue));
    }

    IEnumerator CompleteTrash(GameObject tissue)
    {
        if (audioSource != null && soundTrashDrop != null)
            audioSource.PlayOneShot(soundTrashDrop);

        // Animation simple : tissu retrecit et disparait
        Vector3 originalScale = tissue.transform.localScale;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            tissue.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        tissue.SetActive(false);
        tissue.transform.localScale = originalScale;

        isDryingActive = false;
        glowActive     = false;
        if (tabLight != null) tabLight.enabled = false;
        if (glowMat != null) SetGlow(0f);

        Debug.Log("[Drying] Tissu jete — sechage complet.");
        HandWashingManager.Instance?.CompleteDryingStep();
    }

    void SetGlow(float intensity)
    {
        if (glowMat == null) return;
        glowMat.SetColor(EmissionColor, baseGlowColor * intensity);
    }
}
