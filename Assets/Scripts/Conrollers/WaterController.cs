using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WaterController : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem waterParticles;
    public AudioSource waterSound;

    [Header("Duration under water before validating the step")]
    public float waterValidationDuration = 2f;

    [Header("Hand layer")]
    public string handLayerName = "PlayerHand";

    private float waterTimer = 0f;
    private bool waterStepValidated = false;
    private bool waterRunning = false;
    private readonly HashSet<string> handsInside = new HashSet<string>();

    public static event System.Action OnHandUnderWater;
    public static event System.Action OnHandLeftWater;

    private void OnEnable()
    {
        HandWashingManager.OnStepChanged += OnStepChanged;
        HandWashingManager.OnContamination += OnContamination;
    }

    private void OnDisable()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
        HandWashingManager.OnContamination -= OnContamination;
    }

    private void Start()
    {
        int handLayer = LayerMask.NameToLayer(handLayerName);
        if (handLayer == -1)
            Debug.LogWarning("Layer '" + handLayerName + "' not found. Hand detection will use names/components.");
        else
            Debug.Log("Layer '" + handLayerName + "' found, index: " + handLayer);

        StopWater();
    }

    private void Update()
    {
        bool isWettingStep = HandWashingManager.Instance != null &&
                             HandWashingManager.Instance.CurrentStep == HandWashingManager.WashStep.WettingHands;

        if (waterRunning && isWettingStep && !waterStepValidated)
        {
            waterTimer += Time.deltaTime;

            if (waterTimer >= waterValidationDuration)
            {
                waterStepValidated = true;
                Debug.Log("Water step validated after " + waterValidationDuration + "s.");
                HandWashingManager.Instance?.CompleteWettingStep();
            }
        }

        if (Keyboard.current == null) return;

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("TEST K -> StartWater()");
            StartWater();
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Debug.Log("TEST L -> StopWater()");
            StopWater();
        }

        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            Debug.Log("TEST V -> force validate water step");
            waterStepValidated = true;
            HandWashingManager.Instance?.CompleteWettingStep();
        }
    }

    private void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.WettingHands ||
            step == HandWashingManager.WashStep.RinsingHands)
        {
            handsInside.Clear();
            waterTimer = 0f;
            waterStepValidated = false;
            StopWater();
            return;
        }

        handsInside.Clear();
        StopWater();
    }

    private void OnContamination()
    {
        handsInside.Clear();
        waterTimer = 0f;
        waterStepValidated = false;
        StopWater();
        OnHandLeftWater?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Water trigger enter: " + other.gameObject.name
                  + " | Parent: " + (other.transform.parent != null ? other.transform.parent.name : "none")
                  + " | Layer: " + LayerMask.LayerToName(other.gameObject.layer));

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

        Debug.Log("Hand left water: " + handId + " | Remaining hands: " + handsInside.Count);

        if (handsInside.Count == 0)
        {
            StopWater();
            OnHandLeftWater?.Invoke();

            if (!waterStepValidated)
            {
                waterTimer = 0f;
                Debug.Log("Water timer reset because hand left before validation.");
            }
        }
    }

    private void RegisterHandContact(Collider other)
    {
        if (!HandContactUtility.IsHandOrController(other, handLayerName)) return;

        string handId = HandContactUtility.GetHandId(other);
        if (handsInside.Contains(handId)) return;

        handsInside.Add(handId);
        Debug.Log("Hand detected under water: " + handId + " | Hands inside: " + handsInside.Count);

        if (handsInside.Count == 1)
        {
            StartWater();
            OnHandUnderWater?.Invoke();
        }
    }

    private void StartWater()
    {
        waterRunning = true;

        if (waterParticles != null)
            waterParticles.Play();
        else
            Debug.LogWarning("waterParticles is not assigned in the Inspector.");

        if (waterSound != null && !waterSound.isPlaying)
            waterSound.Play();

        Debug.Log("Water started.");
    }

    private void StopWater()
    {
        waterRunning = false;

        if (waterParticles != null)
            waterParticles.Stop();

        if (waterSound != null)
            waterSound.Stop();
    }
}
