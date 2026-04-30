using UnityEngine;

public class SoapController : MonoBehaviour
{
    [Header("Feedback visuel — Halo bleu pulsant (rapport section 2.1.1)")]
    public Light soapHaloLight;         // lumière point sur le distributeur
    public float pulseSpeed = 2f;       // vitesse du pulsement
    public float pulseMinIntensity = 0.5f;
    public float pulseMaxIntensity = 3f;

    [Header("Animation liquide savon")]
    public ParticleSystem soapLiquidParticles;  // jet de liquide qui sort du distributeur
    public AudioSource soapPumpSound;           // son optionnel "pchhht" du distributeur

    [Header("Mousse sur les mains")]
    public GameObject foamLeftHand;
    public GameObject foamRightHand;

    [Header("Timing")]
    public float soapContactDuration = 1.5f;
    private float soapTimer = 0f;
    private bool soapZoneActive = false;
    private bool soapApplied = false;
    private bool handInZone = false;

    void Start()
    {
        if (foamLeftHand != null)  foamLeftHand.SetActive(false);
        if (foamRightHand != null) foamRightHand.SetActive(false);
        if (soapHaloLight != null) soapHaloLight.enabled = false;
        if (soapLiquidParticles != null) soapLiquidParticles.Stop();

        HandWashingManager.OnStepChanged += OnStepChanged;
    }

    void OnDestroy()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
    }

    void Update()
    {
        // Pulsement du halo bleu quand la zone savon est active
        if (soapZoneActive && soapHaloLight != null && soapHaloLight.enabled)
        {
            float pulse = Mathf.Lerp(pulseMinIntensity, pulseMaxIntensity,
                          (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            soapHaloLight.intensity = pulse;
        }

        // Timer de contact dans la zone savon
        if (handInZone && soapZoneActive && !soapApplied)
        {
            soapTimer += Time.deltaTime;
            if (soapTimer >= soapContactDuration)
                ApplySoap();
        }
    }

    void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.TakingSoap)
        {
            soapZoneActive = true;
            if (soapHaloLight != null) soapHaloLight.enabled = true;
            Debug.Log("🧴 Halo bleu activé sur distributeur savon");
        }
        else
        {
            soapZoneActive = false;
            if (soapHaloLight != null) soapHaloLight.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("🔵 Soap Zone Enter : " + other.name);
        if (!soapZoneActive || soapApplied) return;
        if (IsHandOrController(other))
        {
            handInZone = true;
            soapTimer = 0f;
            StartSoapLiquid();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsHandOrController(other))
        {
            handInZone = false;
            soapTimer = 0f;
            StopSoapLiquid();
        }
    }

    void StartSoapLiquid()
    {
        if (soapLiquidParticles != null && !soapLiquidParticles.isPlaying)
        {
            soapLiquidParticles.Play();
            Debug.Log("🧴 Liquide savon → ON");
        }
        if (soapPumpSound != null && !soapPumpSound.isPlaying)
            soapPumpSound.Play();
    }

    void StopSoapLiquid()
    {
        if (soapLiquidParticles != null && soapLiquidParticles.isPlaying)
        {
            soapLiquidParticles.Stop();
            Debug.Log("🧴 Liquide savon → OFF");
        }
        if (soapPumpSound != null && soapPumpSound.isPlaying)
            soapPumpSound.Stop();
    }

    void ApplySoap()
    {
        soapApplied = true;
        soapZoneActive = false;
        handInZone = false;
        if (soapHaloLight != null) soapHaloLight.enabled = false;
        StopSoapLiquid();

        if (foamLeftHand != null)  foamLeftHand.SetActive(true);
        if (foamRightHand != null) foamRightHand.SetActive(true);

        Debug.Log("🧴 Savon appliqué — mousse visible");
        HandWashingManager.Instance?.CompleteSoapStep();
    }

    private bool IsHandOrController(Collider other)
    {
        string n = other.gameObject.name.ToLower();
        return n.Contains("controller") || n.Contains("hand")
            || n.Contains("anchor") || n.Contains("index");
    }
}
