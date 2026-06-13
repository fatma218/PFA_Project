using UnityEngine;

public class Contamination : MonoBehaviour
{
    [Header("Identification de la surface")]
    [Tooltip("Nom affiche dans les logs et transmis au HandWashingManager")]
    public string surfaceName = "Surface non sterile";

    [Header("Comportement")]
    [Tooltip("Ignore Idle et Complete")]
    public bool activeOnlyDuringWash = true;
    [Tooltip("Premiere etape ou cette surface devient non sterile")]
    public HandWashingManager.WashStep firstContaminatingStep = HandWashingManager.WashStep.RinsingHands;
    public string handLayerName = "PlayerHand";

    [Header("Feedback visuel optionnel")]
    public Renderer surfaceRenderer;
    public Color alertColor = new Color(1f, 0.2f, 0.2f);

    private Color originalColor;
    private bool contaminationSent;

    private void OnEnable()
    {
        HandWashingManager.OnStepChanged += OnStepChanged;
    }

    private void OnDisable()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
    }

    private void Start()
    {
        if (surfaceRenderer != null)
            originalColor = surfaceRenderer.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (contaminationSent) return;
        if (!HandContactUtility.IsHandOrController(other, handLayerName)) return;

        HandWashingManager manager = HandWashingManager.Instance;
        if (manager == null) return;

        if (activeOnlyDuringWash && !IsContaminatingStep(manager.CurrentStep))
            return;

        Debug.LogWarning("Contamination - " + surfaceName + " touche par " + other.name);
        contaminationSent = true;
        manager.TriggerContamination(surfaceName);

        if (surfaceRenderer != null)
            surfaceRenderer.material.color = alertColor;
    }

    private void OnTriggerExit(Collider other)
    {
        if (HandContactUtility.IsHandOrController(other, handLayerName) && surfaceRenderer != null)
            surfaceRenderer.material.color = originalColor;
    }

    private bool IsContaminatingStep(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.Idle ||
            step == HandWashingManager.WashStep.Complete)
        {
            return false;
        }

        return step >= firstContaminatingStep;
    }

    private void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.Idle ||
            step == HandWashingManager.WashStep.WettingHands)
        {
            contaminationSent = false;
        }
    }
}
