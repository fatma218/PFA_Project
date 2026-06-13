using UnityEngine;

public class ScrubbingController : MonoBehaviour
{
    [Header("Mains (assigner dans Inspector)")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Détection de mouvement")]
    [Tooltip("Vitesse minimale (m/s) pour compter comme frottage actif — réduire si Quest 3 ne détecte pas")]
    public float scrubVelocityThreshold = 0.08f;
    [Tooltip("Secondes sans mouvement avant que le chrono soit mis en pause")]
    public float motionTimeout = 2f;
    [Tooltip("Si activé : le timer tourne dès que l'étape commence, sans attendre de mouvement (mode simplifié)")]
    public bool freeTimerMode = false;

    [Header("Feedback visuel (optionnel)")]
    public ParticleSystem scrubParticlesLeft;
    public ParticleSystem scrubParticlesRight;
    public bool useScrubParticles = false;

    private bool isActive = false;
    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private float timeSinceLastMotion = 0f;

    // Watchdog : si les mains assignées ne bougent pas du tout (objet mal référencé sur Quest),
    // on bascule automatiquement en freeTimerMode après DeadHandTimeout secondes.
    private float _deadHandWatchdog = 0f;
    private bool _watchdogActive = false;
    private Vector3 _watchdogRefLeft;
    private Vector3 _watchdogRefRight;
    private const float DeadHandTimeout = 5f;
    private const float DeadHandEpsilon = 0.001f;

    void Start()
    {
        HandWashingManager.OnStepChanged += OnStepChanged;
    }

    void OnDestroy()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
    }

    void OnStepChanged(HandWashingManager.WashStep step)
    {
        isActive = (step == HandWashingManager.WashStep.ScrubbingHands);

        if (isActive)
        {
            timeSinceLastMotion = 0f;
            RecordHandPositions();

            // Si des mains sont assignées, pause le chrono jusqu'au premier mouvement
            if ((leftHand != null || rightHand != null) && HandWashingManager.Instance != null)
                HandWashingManager.Instance.isActivelyScrubbing = false;

            // Démarre le watchdog : si aucun mouvement détecté après DeadHandTimeout → freeTimerMode
            _deadHandWatchdog = 0f;
            _watchdogActive = (leftHand != null || rightHand != null) && !freeTimerMode;
            _watchdogRefLeft  = leftHand  != null ? leftHand.position  : Vector3.zero;
            _watchdogRefRight = rightHand != null ? rightHand.position : Vector3.zero;

            Debug.Log("✋ ScrubbingController actif — en attente de mouvement des mains");
        }
        else
        {
            StopParticles();
        }
    }

    void Update()
    {
        if (!isActive) return;

        // Watchdog : mains assignées mais immobiles → probable mauvaise référence sur Quest
        if (_watchdogActive && !freeTimerMode)
        {
            _deadHandWatchdog += Time.deltaTime;
            bool leftStill  = leftHand  == null || (leftHand.position  - _watchdogRefLeft).magnitude  < DeadHandEpsilon;
            bool rightStill = rightHand == null || (rightHand.position - _watchdogRefRight).magnitude < DeadHandEpsilon;

            if (_deadHandWatchdog >= DeadHandTimeout && leftStill && rightStill)
            {
                freeTimerMode = true;
                _watchdogActive = false;
                if (HandWashingManager.Instance != null)
                    HandWashingManager.Instance.isActivelyScrubbing = true;
                Debug.LogWarning("ScrubbingController: mains statiques détectées → freeTimerMode activé automatiquement");
            }
        }

        // Mode simplifié : timer libre sans détection de mouvement
        if (freeTimerMode)
        {
            if (HandWashingManager.Instance != null)
                HandWashingManager.Instance.isActivelyScrubbing = true;
            return;
        }

        // Sans mains assignées : on laisse HandWashingManager tourner son timer librement
        if (leftHand == null && rightHand == null)
        {
            if (HandWashingManager.Instance != null)
                HandWashingManager.Instance.isActivelyScrubbing = true;
            return;
        }

        float leftVelocity = 0f;
        float rightVelocity = 0f;

        if (leftHand != null)
        {
            leftVelocity = (leftHand.position - lastLeftPos).magnitude / Time.deltaTime;
            lastLeftPos = leftHand.position;
        }

        if (rightHand != null)
        {
            rightVelocity = (rightHand.position - lastRightPos).magnitude / Time.deltaTime;
            lastRightPos = rightHand.position;
        }

        bool motionDetected = Mathf.Max(leftVelocity, rightVelocity) >= scrubVelocityThreshold;

        if (motionDetected)
        {
            timeSinceLastMotion = 0f;
            PlayParticles();
        }
        else
        {
            timeSinceLastMotion += Time.deltaTime;
            if (timeSinceLastMotion >= motionTimeout)
                StopParticles();
        }

        bool activelyScrubbing = timeSinceLastMotion < motionTimeout;

        if (HandWashingManager.Instance != null)
            HandWashingManager.Instance.isActivelyScrubbing = activelyScrubbing;

        if (activelyScrubbing)
            Debug.Log("🤲 Frottage actif — L:" + leftVelocity.ToString("F2") + "m/s  R:" + rightVelocity.ToString("F2") + "m/s");
    }

    private void RecordHandPositions()
    {
        if (leftHand != null)  lastLeftPos  = leftHand.position;
        if (rightHand != null) lastRightPos = rightHand.position;
    }

    private void PlayParticles()
    {
        if (!useScrubParticles) return;

        if (scrubParticlesLeft  != null && !scrubParticlesLeft.isPlaying)  scrubParticlesLeft.Play();
        if (scrubParticlesRight != null && !scrubParticlesRight.isPlaying) scrubParticlesRight.Play();
    }

    private void StopParticles()
    {
        if (!useScrubParticles) return;

        if (scrubParticlesLeft  != null && scrubParticlesLeft.isPlaying)  scrubParticlesLeft.Stop();
        if (scrubParticlesRight != null && scrubParticlesRight.isPlaying) scrubParticlesRight.Stop();
    }
}
