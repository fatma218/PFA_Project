using UnityEngine;

public class Contamination : MonoBehaviour
{
    [Header("Identification de la surface")]
    [Tooltip("Nom affiché dans les logs et passé à HandWashingManager")]
    public string surfaceName = "Surface non stérile";

    [Header("Comportement")]
    [Tooltip("Ne déclenche la contamination que pendant un lavage actif (ignore Idle et Complete)")]
    public bool activeOnlyDuringWash = true;

    [Header("Feedback visuel (optionnel)")]
    public Renderer surfaceRenderer;
    public Color alertColor = new Color(1f, 0.2f, 0.2f);

    private Color originalColor;

    void Start()
    {
        if (surfaceRenderer != null)
            originalColor = surfaceRenderer.material.color;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsHandOrController(other)) return;

        var mgr = HandWashingManager.Instance;
        if (mgr == null) return;

        if (activeOnlyDuringWash)
        {
            var step = mgr.CurrentStep;
            if (step == HandWashingManager.WashStep.Idle ||
                step == HandWashingManager.WashStep.Complete)
                return;
        }

        Debug.LogWarning("☠️ Contamination — " + surfaceName + " touché par " + other.name);
        mgr.TriggerContamination(surfaceName);

        if (surfaceRenderer != null)
            surfaceRenderer.material.color = alertColor;
    }

    void OnTriggerExit(Collider other)
    {
        if (IsHandOrController(other) && surfaceRenderer != null)
            surfaceRenderer.material.color = originalColor;
    }

    private bool IsHandOrController(Collider other)
    {
        string n = other.gameObject.name.ToLower();
        return n.Contains("controller") || n.Contains("hand")
            || n.Contains("anchor")    || n.Contains("index")
            || n.Contains("left")      || n.Contains("right");
    }
}
