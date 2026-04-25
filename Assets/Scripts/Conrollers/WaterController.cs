using UnityEngine;
using UnityEngine.InputSystem;

public class WaterController : MonoBehaviour
{
    [Header("Références")]
    public ParticleSystem waterParticles;
    public AudioSource waterSound;

    [Header("Détection — laisse vide pour tout accepter")]
    public string handLayerName = "PlayerHand";

    private int handLayer;
    private int handsInsideTrigger = 0;

    public static event System.Action OnHandUnderWater;
    public static event System.Action OnHandLeftWater;

    void Start()
    {
        handLayer = LayerMask.NameToLayer(handLayerName);

        if (handLayer == -1)
            Debug.LogWarning("⚠️ Layer '" + handLayerName + "' introuvable — détection élargie activée.");
        else
            Debug.Log("✅ Layer '" + handLayerName + "' trouvé → index : " + handLayer);

        StopWater();
    }

    void OnTriggerEnter(Collider other)
    {
        // Log pour TOUT ce qui entre — très important pour debugger
        Debug.Log("🔵 Trigger Enter : " + other.gameObject.name 
                  + " | Layer : " + LayerMask.LayerToName(other.gameObject.layer));

        if (IsHandOrController(other))
        {
            handsInsideTrigger++;
            Debug.Log("✋ Détecté ! Compteur : " + handsInsideTrigger);

            if (handsInsideTrigger == 1)
            {
                StartWater();
                OnHandUnderWater?.Invoke();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("🔴 Trigger Exit : " + other.gameObject.name);

        if (IsHandOrController(other))
        {
            handsInsideTrigger--;
            Debug.Log("✋ Sorti. Compteur : " + handsInsideTrigger);

            if (handsInsideTrigger == 0)
            {
                StopWater();
                OnHandLeftWater?.Invoke();
                HandWashingManager.Instance?.CompleteWettingStep();
            }
        }
    }

    private bool IsHandOrController(Collider other)
    {
        string objName = other.gameObject.name.ToLower();
        string layerName = LayerMask.LayerToName(other.gameObject.layer);

        // ✅ Cas 1 : layer PlayerHand configuré correctement (casque réel)
        bool isOnHandLayer = (handLayer != -1 && other.gameObject.layer == handLayer);

        // ✅ Cas 2 : Simulator — controllers Meta ont ces noms typiques
        bool isSimulatorController = objName.Contains("controller")
                                  || objName.Contains("hand")
                                  || objName.Contains("anchor")
                                  || objName.Contains("pointer")
                                  || objName.Contains("index")
                                  || objName.Contains("left")
                                  || objName.Contains("right");

        // ✅ Cas 3 : layer s'appelle "Hands" ou "Controller" (nommage Meta par défaut)
        bool isHandLayer = layerName == "Hands" 
                        || layerName == "Controller"
                        || layerName == "PlayerHand";

        return isOnHandLayer || isSimulatorController || isHandLayer;
    }

    void Update()
    {
        // Test manuel — touche K pour StartWater, L pour StopWater
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("⌨️ TEST K → StartWater()");
            StartWater();
        }
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Debug.Log("⌨️ TEST L → StopWater()");
            StopWater();
        }
    }

    void StartWater()
    {
        if (waterParticles != null) waterParticles.Play();
        else Debug.LogWarning("⚠️ waterParticles non assigné dans l'Inspector !");

        if (waterSound != null) waterSound.Play();
        Debug.Log("💧 StartWater() appelé");
    }

    void StopWater()
    {
        if (waterParticles != null) waterParticles.Stop();
        if (waterSound != null) waterSound.Stop();
    }
}