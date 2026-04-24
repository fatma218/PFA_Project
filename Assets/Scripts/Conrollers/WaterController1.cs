using UnityEngine;

public class WaterController : MonoBehaviour
{
    [Header("Références")]
    public ParticleSystem waterParticles;
    public AudioSource waterSound;

    [Header("Détection")]
    public string handLayerName = "PlayerHand";

    private int handLayer;
    private int handsInsideTrigger = 0; // compte les mains dans la zone

    // Event — d'autres scripts peuvent s'y abonner
    public static event System.Action OnHandUnderWater;
    public static event System.Action OnHandLeftWater;

    void Start()
    {
        handLayer = LayerMask.NameToLayer(handLayerName);
        StopWater(); // eau éteinte au départ
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == handLayer)
        {
            handsInsideTrigger++;
            if (handsInsideTrigger == 1) // première main qui entre
            {
                StartWater();
                OnHandUnderWater?.Invoke();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == handLayer)
        {
            handsInsideTrigger--;
            if (handsInsideTrigger <= 0)
            {
                handsInsideTrigger = 0;
                StopWater();
                OnHandLeftWater?.Invoke();
            }
        }
    }

    void StartWater()
    {
        if (waterParticles != null) waterParticles.Play();
        if (waterSound != null)    waterSound.Play();
    }

    void StopWater()
    {
        if (waterParticles != null) waterParticles.Stop();
        if (waterSound != null)    waterSound.Stop();
    }
}