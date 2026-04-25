using UnityEngine;

public class ScrubbingController : MonoBehaviour
{
    [Header("Mains (assigner dans Inspector)")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Détection de mouvement")]
    [Tooltip("Vitesse minimale (m/s) pour compter comme frottage actif")]
    public float scrubVelocityThreshold = 0.15f;
    [Tooltip("Secondes sans mouvement avant que le chrono soit mis en pause")]
    public float motionTimeout = 2f;

    [Header("Feedback visuel (optionnel)")]
    public ParticleSystem scrubParticlesLeft;
    public ParticleSystem scrubParticlesRight;

    private bool isActive = false;
    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private float timeSinceLastMotion = 0f;

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

        // Sans mains assignées : on laisse HandWashingManager tourner son timer librement
        if (leftHand == null && rightHand == null) return;

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
        if (scrubParticlesLeft  != null && !scrubParticlesLeft.isPlaying)  scrubParticlesLeft.Play();
        if (scrubParticlesRight != null && !scrubParticlesRight.isPlaying) scrubParticlesRight.Play();
    }

    private void StopParticles()
    {
        if (scrubParticlesLeft  != null && scrubParticlesLeft.isPlaying)  scrubParticlesLeft.Stop();
        if (scrubParticlesRight != null && scrubParticlesRight.isPlaying) scrubParticlesRight.Stop();
    }
}
