using System.Collections.Generic;
using UnityEngine;

public class SoapController : MonoBehaviour
{
    [Header("Visual feedback")]
    public Light soapHaloLight;                  // optionnel — fonctionne en Simulator
    public Renderer soapGlowRenderer;            // fallback Quest : objet avec matériau émissif
    public float pulseSpeed = 2f;
    public float pulseMinIntensity = 0.5f;
    public float pulseMaxIntensity = 3f;
    private Material _glowMatInstance;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Color _baseEmission = new Color(0.2f, 0.8f, 1f);

    [Header("Soap liquid animation")]
    public ParticleSystem soapLiquidParticles;
    public AudioSource soapPumpSound;

    [Header("Foam on hands")]
    public GameObject foamLeftHand;
    public GameObject foamRightHand;
    public bool useFoamObjects = false;

    [Header("Timing")]
    public float soapContactDuration = 1.5f;

    [Header("Hand layer")]
    public string handLayerName = "PlayerHand";

    private float soapTimer = 0f;
    private bool soapZoneActive = false;
    private bool soapApplied = false;
    private readonly HashSet<string> handsInside = new HashSet<string>();

    private void Start()
    {
        if (foamLeftHand != null) foamLeftHand.SetActive(false);
        if (foamRightHand != null) foamRightHand.SetActive(false);
        if (soapHaloLight != null) soapHaloLight.enabled = false;
        if (soapLiquidParticles != null) soapLiquidParticles.Stop();

        // Prépare l'émission du matériau (fallback Quest 3)
        if (soapGlowRenderer != null)
        {
            _glowMatInstance = soapGlowRenderer.material; // instance propre
            _glowMatInstance.EnableKeyword("_EMISSION");  // obligatoire sur Android/Quest
            SetGlowEmission(0f);
        }

        HandWashingManager.OnStepChanged += OnStepChanged;
        HandWashingManager.OnContamination += OnContamination;
    }

    private void OnDestroy()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
        HandWashingManager.OnContamination -= OnContamination;
    }

    private void Update()
    {
        if (soapZoneActive)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float pulse = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity, t);

            // Lumière classique (Simulator / PC)
            if (soapHaloLight != null && soapHaloLight.enabled)
                soapHaloLight.intensity = pulse;

            // Émission matériau (Quest 3)
            if (_glowMatInstance != null)
                SetGlowEmission(pulse);
        }

        if (handsInside.Count > 0 && soapZoneActive && !soapApplied)
        {
            soapTimer += Time.deltaTime;
            if (soapTimer >= soapContactDuration)
                ApplySoap();
        }
    }

    private void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.TakingSoap)
        {
            soapZoneActive = true;
            soapApplied = false;
            soapTimer = 0f;
            handsInside.Clear();

            if (soapHaloLight != null)
                soapHaloLight.enabled = true;

            // Allume l'émission du glow (Quest 3)
            if (_glowMatInstance != null)
                SetGlowEmission(pulseMinIntensity);

            Debug.Log("Soap zone activated.");
            return;
        }

        soapZoneActive = false;
        soapTimer = 0f;
        handsInside.Clear();
        StopSoapLiquid();

        if (soapHaloLight != null)
            soapHaloLight.enabled = false;

        if (_glowMatInstance != null)
            SetGlowEmission(0f);
    }

    private void SetGlowEmission(float intensity)
    {
        if (_glowMatInstance == null) return;
        _glowMatInstance.SetColor(EmissionColor, _baseEmission * intensity);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Soap trigger enter: " + other.name);
        RegisterHandContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterHandContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HandContactUtility.IsHandOrController(other, handLayerName)) return;

        string handId = HandContactUtility.GetHandId(other);
        if (!handsInside.Remove(handId)) return;

        if (handsInside.Count == 0)
        {
            soapTimer = 0f;
            StopSoapLiquid();
        }
    }

    private void RegisterHandContact(Collider other)
    {
        if (!soapZoneActive || soapApplied) return;
        if (!HandContactUtility.IsHandOrController(other, handLayerName)) return;

        string handId = HandContactUtility.GetHandId(other);
        if (handsInside.Contains(handId)) return;

        handsInside.Add(handId);
        soapTimer = 0f;
        StartSoapLiquid();
    }

    private void StartSoapLiquid()
    {
        if (soapLiquidParticles != null && !soapLiquidParticles.isPlaying)
        {
            soapLiquidParticles.Play();
            Debug.Log("Soap liquid ON.");
        }

        if (soapPumpSound != null && !soapPumpSound.isPlaying)
            soapPumpSound.Play();
    }

    private void StopSoapLiquid()
    {
        if (soapLiquidParticles != null && soapLiquidParticles.isPlaying)
        {
            soapLiquidParticles.Stop();
            Debug.Log("Soap liquid OFF.");
        }

        if (soapPumpSound != null && soapPumpSound.isPlaying)
            soapPumpSound.Stop();
    }

    private void ApplySoap()
    {
        soapApplied = true;
        soapZoneActive = false;
        handsInside.Clear();

        if (soapHaloLight != null)
            soapHaloLight.enabled = false;

        StopSoapLiquid();

        if (useFoamObjects && foamLeftHand != null) foamLeftHand.SetActive(true);
        if (useFoamObjects && foamRightHand != null) foamRightHand.SetActive(true);

        Debug.Log("Soap applied.");
        HandWashingManager.Instance?.CompleteSoapStep();
    }

    private void OnContamination()
    {
        soapZoneActive = false;
        soapTimer = 0f;
        handsInside.Clear();
        StopSoapLiquid();

        if (soapHaloLight != null)
            soapHaloLight.enabled = false;

        if (_glowMatInstance != null)
            SetGlowEmission(0f);

        if (useFoamObjects && foamLeftHand != null) foamLeftHand.SetActive(false);
        if (useFoamObjects && foamRightHand != null) foamRightHand.SetActive(false);
    }
}
