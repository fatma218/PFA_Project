using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HandWashingManager : MonoBehaviour
{
    [Header("Mode test (à décocher en prod quand bouton UI prêt)")]
    [Tooltip("Démarre automatiquement le lavage au lancement de la scène")]
    public bool autoStartOnPlay = true;
    [Tooltip("Touche clavier pour relancer manuellement (Simulator)")]
    public Key restartKey = Key.R;
    [Tooltip("Touche clavier pour démarrer manuellement (Simulator)")]
    public Key startKey = Key.S;

    public enum WashStep
    {
        Idle,
        WettingHands,       // Mouiller les mains
        TakingSoap,         // Prendre le savon
        ScrubbingHands,     // Frotter les mains (90s min)
        RinsingHands,       // Rincer les mains
        DryingHands,        // Sécher avec tissu stérile
        Complete            // Lavage réussi
    }

    public static HandWashingManager Instance { get; private set; }
    public WashStep CurrentStep { get; private set; } = WashStep.Idle;

    [Header("Chronomètre global")]
    public float globalTimer = 0f;
    public bool timerRunning = false;

    [Header("Frottage — durée minimale")]
    public float scrubbingMinDuration = 90f;
    private float scrubbingTimer = 0f;
    public bool isActivelyScrubbing = true;
    private float lastScrubLogTime = 0f;

    [Header("UI")]
    public Text timerDisplay;
    public Text scrubProgressDisplay;   // NOUVEAU — affiche "Frottage : 5/10s"
    public Slider scrubProgressBar;     // NOUVEAU — barre de progression
    public GameObject successPanel;
    public GameObject failPanel;

    [Header("Audio — Instructions vocales")]
    public AudioClip voiceWetHands;
    public AudioClip voiceTakeSoap;
    public AudioClip voiceScrub;
    public AudioClip voiceRinse;
    public AudioClip voiceDry;
    public AudioClip voiceContamination;
    public AudioClip soundSuccess;
    public AudioClip soundAlert;

    private AudioSource audioSource;

    public static event System.Action<WashStep> OnStepChanged;
    public static event System.Action OnContamination;
    public static event System.Action OnWashComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (autoStartOnPlay)
        {
            Debug.Log("🟢 AutoStart activé — lancement du lavage");
            StartWashing();
        }

        // Cache la barre de progression au début
        if (scrubProgressBar != null) scrubProgressBar.gameObject.SetActive(false);
        if (scrubProgressDisplay != null) scrubProgressDisplay.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current[startKey].wasPressedThisFrame)
            {
                Debug.Log("⌨️ Touche " + startKey + " → StartWashing()");
                StartWashing();
            }
            if (Keyboard.current[restartKey].wasPressedThisFrame)
            {
                Debug.Log("⌨️ Touche " + restartKey + " → RestartWashing() + StartWashing()");
                RestartWashing();
                StartWashing();
            }
        }

        if (timerRunning)
        {
            globalTimer += Time.deltaTime;
            if (timerDisplay != null)
                timerDisplay.text = Mathf.FloorToInt(globalTimer).ToString() + "s";
        }

        if (CurrentStep == WashStep.ScrubbingHands && isActivelyScrubbing)
        {
            scrubbingTimer += Time.deltaTime;

            // Log seulement 1 fois par seconde pour ne pas spammer la console
            if (Time.time - lastScrubLogTime >= 1f)
            {
                Debug.Log("⏱ Frottage : " + Mathf.FloorToInt(scrubbingTimer) + "s / " + scrubbingMinDuration + "s");
                lastScrubLogTime = Time.time;
            }

            // Mise à jour UI progress bar
            UpdateScrubProgressUI();

            if (scrubbingTimer >= scrubbingMinDuration)
                AdvanceToRinse();
        }
    }

    private void UpdateScrubProgressUI()
    {
        if (scrubProgressBar != null)
            scrubProgressBar.value = scrubbingTimer / scrubbingMinDuration;

        if (scrubProgressDisplay != null)
            scrubProgressDisplay.text = "Frottage : " + Mathf.FloorToInt(scrubbingTimer) + " / " + Mathf.FloorToInt(scrubbingMinDuration) + "s";
    }

    public void StartWashing()
    {
        globalTimer = 0f;
        timerRunning = true;
        ChangeStep(WashStep.WettingHands);
        PlayVoice(voiceWetHands);
        Debug.Log("🚿 Lavage démarré — Chronomètre lancé");
    }

    public void CompleteWettingStep()
    {
        if (CurrentStep != WashStep.WettingHands) return;
        ChangeStep(WashStep.TakingSoap);
        PlayVoice(voiceTakeSoap);
    }

    public void CompleteSoapStep()
    {
        if (CurrentStep != WashStep.TakingSoap) return;
        scrubbingTimer = 0f;
        ChangeStep(WashStep.ScrubbingHands);
        PlayVoice(voiceScrub);

        // Affiche la barre de progression
        if (scrubProgressBar != null) scrubProgressBar.gameObject.SetActive(true);
        if (scrubProgressDisplay != null) scrubProgressDisplay.gameObject.SetActive(true);
    }

    private void AdvanceToRinse()
    {
        if (CurrentStep != WashStep.ScrubbingHands) return;
        PlaySound(soundSuccess);
        ChangeStep(WashStep.RinsingHands);
        PlayVoice(voiceRinse);

        // Cache la barre — on n'en a plus besoin
        if (scrubProgressBar != null) scrubProgressBar.gameObject.SetActive(false);
        if (scrubProgressDisplay != null) scrubProgressDisplay.gameObject.SetActive(false);

        Debug.Log("✅ Frottage terminé — passage au rinçage");
    }

    public void CompleteRinsingStep()
    {
        if (CurrentStep != WashStep.RinsingHands) return;
        ChangeStep(WashStep.DryingHands);
        PlayVoice(voiceDry);
    }

    public void CompleteDryingStep()
    {
        if (CurrentStep != WashStep.DryingHands) return;
        timerRunning = false;
        ChangeStep(WashStep.Complete);
        PlaySound(soundSuccess);
        OnWashComplete?.Invoke();
        if (successPanel != null) successPanel.SetActive(true);
        Debug.Log("🎉 Lavage réussi !");
    }

    public void TriggerContamination(string reason = "surface non stérile")
    {
        Debug.LogWarning("⚠️ CONTAMINATION : " + reason);
        timerRunning = false;
        PlaySound(soundAlert);
        PlayVoice(voiceContamination);
        OnContamination?.Invoke();

        if (failPanel != null) failPanel.SetActive(true);
    }

    public void RestartWashing()
    {
        globalTimer = 0f;
        scrubbingTimer = 0f;
        timerRunning = false;

        if (successPanel != null) successPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);
        if (scrubProgressBar != null) scrubProgressBar.gameObject.SetActive(false);
        if (scrubProgressDisplay != null) scrubProgressDisplay.gameObject.SetActive(false);

        ChangeStep(WashStep.Idle);
        Debug.Log("🔄 Redémarrage du lavage");
    }

    private void ChangeStep(WashStep newStep)
    {
        CurrentStep = newStep;
        Debug.Log("➡️ Nouvelle étape : " + newStep);
        OnStepChanged?.Invoke(newStep);
    }

    private void PlayVoice(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}