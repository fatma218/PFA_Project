using UnityEngine;
using UnityEngine.UI;

public class HandWashingManager : MonoBehaviour
{
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
    public float globalTimer = 0f;          // chrono affiché à l'écran
    public bool timerRunning = false;

    [Header("Frottage — durée minimale")]
    public float scrubbingMinDuration = 90f;
    private float scrubbingTimer = 0f;
    // ScrubbingController met ce flag à false si aucun mouvement détecté
    public bool isActivelyScrubbing = true;

    [Header("UI")]
    public Text timerDisplay;               // affichage du chrono en scène
    public GameObject successPanel;         // panneau "Lavage Réussi"
    public GameObject failPanel;            // panneau rouge "Contamination"

    [Header("Audio — Instructions vocales")]
    public AudioClip voiceWetHands;         // "Mouillez vos mains"
    public AudioClip voiceTakeSoap;         // "Prenez le savon"
    public AudioClip voiceScrub;            // "Frottez vos mains"
    public AudioClip voiceRinse;            // "Rincez vos mains"
    public AudioClip voiceDry;              // "Séchez vos mains avec le tissu stérile"
    public AudioClip voiceContamination;    // "Vous avez touché une surface non stérile"
    public AudioClip soundSuccess;          // bip de validation
    public AudioClip soundAlert;            // son d'alerte contamination

    private AudioSource audioSource;

    // Events pour notifier les autres scripts
    public static event System.Action<WashStep> OnStepChanged;
    public static event System.Action OnContamination;
    public static event System.Action OnWashComplete;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Chronomètre global affiché en scène
        if (timerRunning)
        {
            globalTimer += Time.deltaTime;
            if (timerDisplay != null)
                timerDisplay.text = Mathf.FloorToInt(globalTimer).ToString() + "s";
        }

        // Compteur spécifique à la phase frottage — avance seulement si mouvement détecté
        if (CurrentStep == WashStep.ScrubbingHands && isActivelyScrubbing)
        {
            scrubbingTimer += Time.deltaTime;
            Debug.Log("⏱ Frottage : " + Mathf.FloorToInt(scrubbingTimer) + "s / " + scrubbingMinDuration + "s");

            if (scrubbingTimer >= scrubbingMinDuration)
                AdvanceToRinse();
        }
    }

    // ─── Démarrage ───────────────────────────────────────────────
    public void StartWashing()
    {
        globalTimer = 0f;
        timerRunning = true;
        ChangeStep(WashStep.WettingHands);
        PlayVoice(voiceWetHands);
        Debug.Log("🚿 Lavage démarré — Chronomètre lancé");
    }

    // ─── Transitions entre étapes ────────────────────────────────
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
    }

    // Avancement automatique après 90s de frottage (appelé dans Update)
    private void AdvanceToRinse()
    {
        if (CurrentStep != WashStep.ScrubbingHands) return;
        PlaySound(soundSuccess); // bip de validation
        ChangeStep(WashStep.RinsingHands);
        PlayVoice(voiceRinse);
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

    // ─── Contamination ───────────────────────────────────────────
    public void TriggerContamination(string reason = "surface non stérile")
    {
        Debug.LogWarning("⚠️ CONTAMINATION : " + reason);
        timerRunning = false;
        PlaySound(soundAlert);
        PlayVoice(voiceContamination);
        OnContamination?.Invoke();
        
        // La scène vire au rouge (géré par ContaminationEffect.cs ci-dessous)
        if (failPanel != null) failPanel.SetActive(true);
    }

    // ─── Restart ─────────────────────────────────────────────────
    public void RestartWashing()
    {
        globalTimer = 0f;
        scrubbingTimer = 0f;
        timerRunning = false;

        if (successPanel != null) successPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);

        ChangeStep(WashStep.Idle);
        Debug.Log("🔄 Redémarrage du lavage");
    }

    // ─── Utilitaires ─────────────────────────────────────────────
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
