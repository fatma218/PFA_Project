using UnityEngine;
using UnityEngine.UI;

public class RinsingController : MonoBehaviour
{
    [Header("Mousse à rincer (assigner les ParticleSystems FoamLeft/FoamRight)")]
    public ParticleSystem foamLeftParticles;
    public ParticleSystem foamRightParticles;

    [Header("Timing")]
    [Tooltip("Durée de rinçage requise (mets 5s pour le test, 15-20s en production)")]
    public float rinsingDuration = 5f;
    private float rinsingTimer = 0f;

    [Header("UI (optionnel)")]
    public Slider rinseProgressBar;
    public Text rinseProgressDisplay;

    private bool isRinsingActive = false;
    private bool handInWater = false;
    private float originalFoamLeftRate;
    private float originalFoamRightRate;

    void Start()
    {
        HandWashingManager.OnStepChanged += OnStepChanged;
        WaterController.OnHandUnderWater += OnHandUnderWater;
        WaterController.OnHandLeftWater += OnHandLeftWater;

        if (rinseProgressBar != null) rinseProgressBar.gameObject.SetActive(false);
        if (rinseProgressDisplay != null) rinseProgressDisplay.gameObject.SetActive(false);

        // Mémorise le débit d'origine de la mousse pour pouvoir le restaurer/réduire
        if (foamLeftParticles != null)
            originalFoamLeftRate = foamLeftParticles.emission.rateOverTime.constant;
        if (foamRightParticles != null)
            originalFoamRightRate = foamRightParticles.emission.rateOverTime.constant;
    }

    void OnDestroy()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
        WaterController.OnHandUnderWater -= OnHandUnderWater;
        WaterController.OnHandLeftWater -= OnHandLeftWater;
    }

    void OnStepChanged(HandWashingManager.WashStep step)
    {
        isRinsingActive = (step == HandWashingManager.WashStep.RinsingHands);

        if (isRinsingActive)
        {
            rinsingTimer = 0f;
            if (rinseProgressBar != null) rinseProgressBar.gameObject.SetActive(true);
            if (rinseProgressDisplay != null) rinseProgressDisplay.gameObject.SetActive(true);
            Debug.Log("🚿 Phase Rinçage activée — mets tes mains sous l'eau");
        }
        else
        {
            if (rinseProgressBar != null) rinseProgressBar.gameObject.SetActive(false);
            if (rinseProgressDisplay != null) rinseProgressDisplay.gameObject.SetActive(false);
        }
    }

    void OnHandUnderWater()
    {
        if (!isRinsingActive) return;
        handInWater = true;
        Debug.Log("🚿 Main sous l'eau — rinçage en cours");
    }

    void OnHandLeftWater()
    {
        if (!isRinsingActive) return;
        handInWater = false;
        Debug.Log("⚠️ Main sortie de l'eau — rinçage en pause");
    }

    void Update()
    {
        if (!isRinsingActive || !handInWater) return;

        rinsingTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(rinsingTimer / rinsingDuration);

        // Réduit progressivement la mousse (de 100% à 0%)
        ReduceFoam(1f - progress);

        // UI
        if (rinseProgressBar != null)
            rinseProgressBar.value = progress;
        if (rinseProgressDisplay != null)
            rinseProgressDisplay.text = "Rinçage : " + Mathf.FloorToInt(rinsingTimer) + " / " + Mathf.FloorToInt(rinsingDuration) + "s";

        if (rinsingTimer >= rinsingDuration)
            CompleteRinsing();
    }

    void ReduceFoam(float multiplier)
    {
        if (foamLeftParticles != null)
        {
            var emission = foamLeftParticles.emission;
            emission.rateOverTime = originalFoamLeftRate * multiplier;
        }
        if (foamRightParticles != null)
        {
            var emission = foamRightParticles.emission;
            emission.rateOverTime = originalFoamRightRate * multiplier;
        }
    }

    void CompleteRinsing()
    {
        if (foamLeftParticles != null) foamLeftParticles.Stop();
        if (foamRightParticles != null) foamRightParticles.Stop();

        Debug.Log("✅ Rinçage terminé — mousse enlevée");
        HandWashingManager.Instance?.CompleteRinsingStep();
    }
}