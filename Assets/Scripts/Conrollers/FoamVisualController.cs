using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public class FoamVisualController : MonoBehaviour
{
    [Header("Optional explicit hand renderers")]
    public Renderer[] leftHandRenderers;
    public Renderer[] rightHandRenderers;

    [Header("Foam shader tint")]
    public Color foamColor = new Color(1f, 0.25f, 0.65f, 1f);
    [Range(0f, 1f)] public float foamStrength = 0.75f;
    public float rinseFadeDuration = 2.5f;
    public bool useRinsingDurationForFade = true;

    [Header("Visible fallback soap blobs")]
    public bool useSoapBlobs = true;
    public float palmBlobScale = 0.045f;
    public float fingerBlobScale = 0.022f;

    [Header("Debug")]
    public bool enableDebugFoamKey = true;
    public Key debugFoamKey = Key.F;

    private readonly List<RendererState> rendererStates = new List<RendererState>();
    private readonly List<SoapBlob> soapBlobs = new List<SoapBlob>();
    private MaterialPropertyBlock propertyBlock;
    private Material soapBlobMaterial;
    private Coroutine fadeCoroutine;
    private RinsingController rinsingController;
    private float currentStrength;
    private float rinseFadeElapsed;
    private bool isRinsingStep;
    private bool isHandUnderWaterForRinse;
    private bool forceDebugFoam;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        rinsingController = GetComponent<RinsingController>();
    }

    private void OnEnable()
    {
        HandWashingManager.OnStepChanged += OnStepChanged;
        HandWashingManager.OnContamination += OnContamination;
        WaterController.OnHandUnderWater += OnHandUnderWater;
        WaterController.OnHandLeftWater += OnHandLeftWater;
    }

    private void OnDisable()
    {
        HandWashingManager.OnStepChanged -= OnStepChanged;
        HandWashingManager.OnContamination -= OnContamination;
        WaterController.OnHandUnderWater -= OnHandUnderWater;
        WaterController.OnHandLeftWater -= OnHandLeftWater;
        RestoreHands();
    }

    private void Start()
    {
        CacheHandMaterials();
        CreateSoapBlobs();
        RestoreHands();

        if (HandWashingManager.Instance != null)
            OnStepChanged(HandWashingManager.Instance.CurrentStep);
    }

    private void Update()
    {
        if (!enableDebugFoamKey || Keyboard.current == null) return;

        if (Keyboard.current[debugFoamKey].wasPressedThisFrame)
        {
            StopFade();
            forceDebugFoam = !forceDebugFoam;
            currentStrength = forceDebugFoam ? foamStrength : 0f;

            if (forceDebugFoam)
                ApplyFoam(currentStrength);
            else
                RestoreHands();

            Debug.Log("Test mousse mains : " + (forceDebugFoam ? "ON" : "OFF") +
                      " | Renderers trouves : " + rendererStates.Count +
                      " | Taches savon : " + soapBlobs.Count);
        }
    }

    private void LateUpdate()
    {
        if (currentStrength > 0f || forceDebugFoam)
            ApplyFoam(currentStrength > 0f ? currentStrength : foamStrength);
    }

    private void OnStepChanged(HandWashingManager.WashStep step)
    {
        if (step == HandWashingManager.WashStep.ScrubbingHands)
        {
            StopFade();
            isRinsingStep = false;
            isHandUnderWaterForRinse = false;
            rinseFadeElapsed = 0f;
            forceDebugFoam = false;
            currentStrength = foamStrength;
            ApplyFoam(currentStrength);
            Debug.Log("Mousse appliquee pendant le frottage | Renderers : " +
                      rendererStates.Count + " | Taches savon : " + soapBlobs.Count);
            return;
        }

        if (step == HandWashingManager.WashStep.RinsingHands)
        {
            StopFade();
            isRinsingStep = true;
            isHandUnderWaterForRinse = false;
            rinseFadeElapsed = 0f;
            forceDebugFoam = false;
            currentStrength = foamStrength;
            ApplyFoam(currentStrength);
            Debug.Log("Rincage pret : la mousse reste visible jusqu'a activation de l'eau.");
            return;
        }

        if (step == HandWashingManager.WashStep.Idle ||
            step == HandWashingManager.WashStep.DryingHands ||
            step == HandWashingManager.WashStep.Complete)
        {
            StopFade();
            isRinsingStep = false;
            isHandUnderWaterForRinse = false;
            forceDebugFoam = false;
            currentStrength = 0f;
            RestoreHands();
        }
    }

    private void OnHandUnderWater()
    {
        if (!isRinsingStep) return;

        isHandUnderWaterForRinse = true;

        if (fadeCoroutine == null)
            fadeCoroutine = StartCoroutine(FadeFoamOutWhileWaterRuns());

        Debug.Log("Eau active pendant le rincage : disparition progressive de la mousse.");
    }

    private void OnHandLeftWater()
    {
        if (!isRinsingStep) return;

        isHandUnderWaterForRinse = false;
        Debug.Log("Rincage en pause : la mousse garde son niveau actuel.");
    }

    private void OnContamination()
    {
        StopFade();
        isRinsingStep = false;
        isHandUnderWaterForRinse = false;
        forceDebugFoam = false;
        currentStrength = 0f;
        RestoreHands();
    }

    private void CacheHandMaterials()
    {
        rendererStates.Clear();

        List<Renderer> renderers = new List<Renderer>();
        AddRenderers(renderers, leftHandRenderers);
        AddRenderers(renderers, rightHandRenderers);

        if (renderers.Count == 0)
            FindHandRenderers(renderers);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            List<string> colorProperties = GetColorProperties(renderer);
            if (colorProperties.Count == 0) continue;

            foreach (string colorProperty in colorProperties)
                rendererStates.Add(new RendererState(renderer, colorProperty, GetOriginalColor(renderer, colorProperty)));
        }
    }

    private void AddRenderers(List<Renderer> renderers, Renderer[] source)
    {
        if (source == null) return;

        foreach (Renderer renderer in source)
        {
            if (renderer != null && !renderers.Contains(renderer))
                renderers.Add(renderer);
        }
    }

    private void FindHandRenderers(List<Renderer> renderers)
    {
        foreach (Renderer renderer in FindObjectsOfType<Renderer>(true))
        {
            string path = GetHierarchyPath(renderer.transform).ToLowerInvariant();
            if (!path.Contains("hand")) continue;

            if (path.Contains("left") ||
                path.Contains("right") ||
                path.Contains("ovr") ||
                path.Contains("openxr"))
            {
                renderers.Add(renderer);
            }
        }
    }

    private List<string> GetColorProperties(Renderer renderer)
    {
        List<string> properties = new List<string>();
        string[] knownColorProperties =
        {
            "_BaseColor",
            "_Color",
            "baseColorFactor",
            "_ColorTop",
            "_ColorBottom",
            "_GlowColor",
            "_OutlineColor"
        };

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null) continue;

            foreach (string property in knownColorProperties)
            {
                if (material.HasProperty(property) && !properties.Contains(property))
                    properties.Add(property);
            }
        }

        return properties;
    }

    private void ApplyFoam(float strength)
    {
        if (rendererStates.Count == 0)
            CacheHandMaterials();

        if (useSoapBlobs && soapBlobs.Count == 0)
            CreateSoapBlobs();

        foreach (RendererState state in rendererStates)
        {
            if (state.Renderer == null) continue;

            EnsurePropertyBlock();
            Color tinted = Color.Lerp(state.OriginalColor, foamColor, strength);
            state.Renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(state.ColorProperty, tinted);
            state.Renderer.SetPropertyBlock(propertyBlock);
        }

        SetSoapBlobs(strength);
    }

    private IEnumerator FadeFoamOutWhileWaterRuns()
    {
        float duration = GetRinseFadeDuration();

        while (isRinsingStep && rinseFadeElapsed < duration)
        {
            if (isHandUnderWaterForRinse)
            {
                rinseFadeElapsed += Time.deltaTime;
                currentStrength = Mathf.Lerp(foamStrength, 0f, rinseFadeElapsed / duration);
                ApplyFoam(currentStrength);
            }

            yield return null;
        }

        if (isRinsingStep)
        {
            currentStrength = 0f;
            RestoreHands();
        }

        fadeCoroutine = null;
    }

    private float GetRinseFadeDuration()
    {
        if (useRinsingDurationForFade)
        {
            if (rinsingController != null && rinsingController.rinsingDuration > 0f)
                return rinsingController.rinsingDuration;
        }

        return Mathf.Max(0.1f, rinseFadeDuration);
    }

    private void RestoreHands()
    {
        foreach (RendererState state in rendererStates)
        {
            if (state.Renderer == null) continue;

            EnsurePropertyBlock();
            state.Renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(state.ColorProperty, state.OriginalColor);
            state.Renderer.SetPropertyBlock(propertyBlock);
        }

        SetSoapBlobs(0f);
    }

    private void StopFade()
    {
        if (fadeCoroutine == null) return;

        StopCoroutine(fadeCoroutine);
        fadeCoroutine = null;
    }

    private string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private Color GetOriginalColor(Renderer renderer, string colorProperty)
    {
        foreach (Material material in renderer.sharedMaterials)
        {
            if (material != null && material.HasProperty(colorProperty))
                return material.GetColor(colorProperty);
        }

        return Color.white;
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
    }

    private void CreateSoapBlobs()
    {
        if (!useSoapBlobs || soapBlobs.Count > 0) return;

        EnsureSoapBlobMaterial();

        string[] jointNames =
        {
            "XRHand_Palm",
            "XRHand_Wrist",
            "XRHand_ThumbProximal",
            "XRHand_IndexProximal",
            "XRHand_MiddleProximal",
            "XRHand_RingProximal",
            "XRHand_LittleProximal",
            "XRHand_IndexIntermediate",
            "XRHand_MiddleIntermediate"
        };

        foreach (Transform transformInScene in FindObjectsOfType<Transform>(true))
        {
            if (!IsSoapJoint(transformInScene, jointNames)) continue;

            float scale = transformInScene.name == "XRHand_Palm" ||
                          transformInScene.name == "XRHand_Wrist"
                ? palmBlobScale
                : fingerBlobScale;

            GameObject blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blob.name = "LiquidSoap_" + transformInScene.name;
            blob.layer = transformInScene.gameObject.layer;
            blob.transform.SetParent(transformInScene, false);
            blob.transform.localPosition = Vector3.zero;
            blob.transform.localRotation = Quaternion.identity;
            blob.transform.localScale = Vector3.one * scale;

            Collider blobCollider = blob.GetComponent<Collider>();
            if (blobCollider != null)
                Destroy(blobCollider);

            Renderer blobRenderer = blob.GetComponent<Renderer>();
            if (blobRenderer == null) continue;

            blobRenderer.sharedMaterial = soapBlobMaterial;
            soapBlobs.Add(new SoapBlob(blobRenderer, scale));
        }
    }

    private bool IsSoapJoint(Transform target, string[] jointNames)
    {
        bool isKnownJoint = false;
        foreach (string jointName in jointNames)
        {
            if (target.name == jointName)
            {
                isKnownJoint = true;
                break;
            }
        }

        if (!isKnownJoint) return false;

        string path = GetHierarchyPath(target).ToLowerInvariant();
        return path.Contains("hand") && !path.Contains("controlleranchor");
    }

    private void EnsureSoapBlobMaterial()
    {
        if (soapBlobMaterial != null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        soapBlobMaterial = new Material(shader);
        soapBlobMaterial.name = "Runtime_LiquidSoap_Pink";
        SetMaterialColor(soapBlobMaterial, foamColor);
    }

    private void SetSoapBlobs(float strength)
    {
        if (!useSoapBlobs) return;

        bool visible = strength > 0.03f;
        foreach (SoapBlob blob in soapBlobs)
        {
            if (blob.Renderer == null) continue;

            blob.Renderer.enabled = visible;
            float scale = Mathf.Lerp(blob.BaseScale * 0.35f, blob.BaseScale, strength);
            blob.Renderer.transform.localScale = Vector3.one * scale;
        }
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private class RendererState
    {
        public readonly Renderer Renderer;
        public readonly string ColorProperty;
        public readonly Color OriginalColor;

        public RendererState(Renderer renderer, string colorProperty, Color originalColor)
        {
            Renderer = renderer;
            ColorProperty = colorProperty;
            OriginalColor = originalColor;
        }
    }

    private class SoapBlob
    {
        public readonly Renderer Renderer;
        public readonly float BaseScale;

        public SoapBlob(Renderer renderer, float baseScale)
        {
            Renderer = renderer;
            BaseScale = baseScale;
        }
    }
}
