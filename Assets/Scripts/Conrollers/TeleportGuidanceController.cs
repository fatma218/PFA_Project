using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Affiche un panneau de guidage au demarrage montrant le geste de teleportation.
/// Quand l'utilisateur teleporte devant le lavabo, cache le panneau et lance StartWashing().
/// </summary>
public class TeleportGuidanceController : MonoBehaviour
{
    [Header("References")]
    public HandWashingManager handWashingManager;
    public Transform[] teleportHotspots;       // Les Teleport Hotspot devant le lavabo

    [Header("Panneau de guidage (World Space Canvas)")]
    public GameObject guidancePanel;           // Canvas World Space
    public Image gestureImage;                 // Image du geste de main
    public TextMeshProUGUI guidanceText;        // Texte d'instruction

    [Header("Detection")]
    public float arrivalRadius = 0.8f;         // Distance pour considerer l'arrivee
    public float checkInterval = 0.2f;         // Verif toutes les 200ms

    [Header("Textes")]
    [TextArea] public string instructionText =
        "Regardez les zones bleues au sol\net pointez votre main vers l'une d'elles\npour vous approcher du lavabo";

    private Transform playerHead;
    private bool washingStarted = false;

void Start()
    {
        // Auto-trouver HandWashingManager si pas assigne
        if (handWashingManager == null)
            handWashingManager = FindObjectOfType<HandWashingManager>();

        // Auto-trouver les Teleport Hotspots si pas assignes
        if (teleportHotspots == null || teleportHotspots.Length == 0)
        {
            var list = new System.Collections.Generic.List<Transform>();
            var h1 = GameObject.Find("Teleport Hotspot");
            var h2 = GameObject.Find("Teleport Hotspot (1)");
            if (h1 != null) list.Add(h1.transform);
            if (h2 != null) list.Add(h2.transform);
            teleportHotspots = list.ToArray();
            Debug.Log("[TeleportGuidance] Hotspots trouves auto : " + teleportHotspots.Length);
        }

        // Recuperer la camera (tete du joueur)
        if (Camera.main != null)
            playerHead = Camera.main.transform;
        else
            Debug.LogWarning("[TeleportGuidance] Camera.main introuvable !");

        // S'assurer que le lavage n'est pas encore lance
        if (handWashingManager != null)
        {
            handWashingManager.autoStartOnPlay = false;
            var audio = handWashingManager.GetComponent<AudioSource>();
            if (audio != null) audio.Stop();
            Debug.Log("[TeleportGuidance] HandWashingManager trouve, autoStart desactive");
        }
        else
        {
            Debug.LogError("[TeleportGuidance] HandWashingManager INTROUVABLE !");
        }

        // Afficher le panneau si assigne
        if (guidancePanel != null)
        {
            guidancePanel.SetActive(true);
            if (guidanceText != null)
                guidanceText.text = instructionText;
        }

        // Commencer la verification periodique
        StartCoroutine(CheckPlayerPosition());
        Debug.Log("[TeleportGuidance] Demarrage OK — en attente du teleport joueur");
    }

    IEnumerator CheckPlayerPosition()
    {
        while (!washingStarted)
        {
            yield return new WaitForSeconds(checkInterval);

            if (playerHead == null) continue;

            // Verifier chaque hotspot
            foreach (var hotspot in teleportHotspots)
            {
                if (hotspot == null) continue;

                float dist = Vector2.Distance(
                    new Vector2(playerHead.position.x, playerHead.position.z),
                    new Vector2(hotspot.position.x, hotspot.position.z)
                );

                if (dist <= arrivalRadius)
                {
                    OnPlayerArrivedAtSink();
                    yield break;
                }
            }
        }
    }

    void OnPlayerArrivedAtSink()
    {
        washingStarted = true;

        // Cacher le panneau de guidage avec un fade
        StartCoroutine(HideGuidancePanel());

        // Lancer le son et le lavage
        if (handWashingManager != null)
        {
            handWashingManager.StartWashing();
            Debug.Log("[TeleportGuidance] Joueur arrive au lavabo → StartWashing()");
        }
    }

    IEnumerator HideGuidancePanel()
    {
        if (guidancePanel == null) yield break;

        // Fade out rapide
        CanvasGroup cg = guidancePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = guidancePanel.AddComponent<CanvasGroup>();

        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 2f;
            cg.alpha = t;
            yield return null;
        }

        guidancePanel.SetActive(false);
    }

    // Appelable depuis un bouton UI si besoin de skip le guidage
    public void SkipGuidance()
    {
        StopAllCoroutines();
        OnPlayerArrivedAtSink();
    }
}
