using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SuccessMenuController : MonoBehaviour
{
    [Header("Text")]
    public string title = "LAVAGE REUSSI";
    public string message = "Procedure de lavage validee.";
    public string continueLabel = "Habillage sterile";
    public string restartLabel = "Recommencer";

    [Header("Placement")]
    public float distanceFromCamera = 0.55f;
    public Vector3 cameraOffset = new Vector3(-0.08f, 0.08f, 0f);
    public float worldSpaceScale = 0.00085f;

    [Header("Navigation")]
    [Tooltip("Scene chargee par le bouton de continuation. Change ce nom si ta scene d'habillage a un autre nom.")]
    public string nextSceneName = "PreparationRoom";
    public bool restartCurrentSceneIfNoWashingManager = true;

    [Header("Feedback")]
    public Color panelColor = new Color(0.03f, 0.18f, 0.1f, 0.96f);
    public Color glowColor = new Color(0.05f, 0.75f, 0.35f, 0.28f);
    public bool playFallbackSuccessSound = true;

    private AudioSource audioSource;
    private AudioClip successClip;
    private Font runtimeFont;
    private Text messageText;
    private bool uiBuilt;

    private void Awake()
    {
        EnsureUI();
    }

    private void OnEnable()
    {
        EnsureUI();
        if (Application.isPlaying)
            PlaceInFrontOfCamera();
    }

    public void Show()
    {
        enabled = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureUI();
        RefreshUIOrder();
        PlaceInFrontOfCamera();

        if (messageText != null)
            messageText.text = message;

        if (playFallbackSuccessSound)
            PlayFallbackSuccess();
    }

    public void ContinueToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    public void RestartModule()
    {
        HandWashingManager washingManager = HandWashingManager.Instance;
        if (washingManager != null)
        {
            gameObject.SetActive(false);
            washingManager.RestartWashing();
            washingManager.StartWashing();
            return;
        }

        if (restartCurrentSceneIfNoWashingManager)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void EnsureUI()
    {
        if (uiBuilt)
            return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 520;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.dynamicPixelsPerUnit = 12f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        EnsureTrackedDeviceRaycaster();
        EnsureEventSystem();

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.sizeDelta = new Vector2(1050f, 600f);
            root.localScale = Vector3.one * worldSpaceScale;
        }

        GameObject glow = GetOrCreateChild("SuccessGlow", transform, typeof(RectTransform), typeof(Image));
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        ConfigureRect(glowRect, Vector2.zero, new Vector2(1100f, 650f));
        Image glowImage = glow.GetComponent<Image>();
        glowImage.color = glowColor;
        glowImage.raycastTarget = false;
        glow.transform.SetAsFirstSibling();

        GameObject panel = GetOrCreateChild("SuccessMenuPanel", transform, typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        ConfigureRect(panelRect, Vector2.zero, new Vector2(860f, 440f));
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = panelColor;
        panel.transform.SetAsLastSibling();

        Text iconText = GetOrCreateText(panel.transform, "SuccessSign", "OK", 58, new Vector2(0f, 145f), new Vector2(180f, 90f), new Color(0.2f, 1f, 0.55f, 1f));
        iconText.fontStyle = FontStyle.Bold;

        Text titleText = GetOrCreateText(panel.transform, "Title", title, 50, new Vector2(0f, 70f), new Vector2(760f, 70f), Color.white);
        titleText.fontStyle = FontStyle.Bold;

        messageText = GetOrCreateText(panel.transform, "Message", message, 30, new Vector2(0f, -20f), new Vector2(760f, 105f), Color.white);

        GetOrCreateButton(panel.transform, "ContinueButton", continueLabel, new Vector2(-185f, -150f), ContinueToNextScene, new Color(0.02f, 0.45f, 0.2f, 1f), SuccessMenuButtonTrigger.ButtonAction.Continue);
        GetOrCreateButton(panel.transform, "RestartButton", restartLabel, new Vector2(185f, -150f), RestartModule, new Color(0.04f, 0.23f, 0.7f, 1f), SuccessMenuButtonTrigger.ButtonAction.Restart);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        uiBuilt = true;
    }

    private void RefreshUIOrder()
    {
        Transform glow = transform.Find("SuccessGlow");
        if (glow != null)
            glow.SetAsFirstSibling();

        Transform panel = transform.Find("SuccessMenuPanel");
        if (panel != null)
            panel.SetAsLastSibling();
    }

    private GameObject GetOrCreateChild(string childName, Transform parent, params Type[] components)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);

            foreach (Type componentType in components)
            {
                if (existing.GetComponent(componentType) == null)
                    existing.gameObject.AddComponent(componentType);
            }

            return existing.gameObject;
        }

        GameObject child = new GameObject(childName, components);
        child.transform.SetParent(parent, false);
        return child;
    }

    private Text GetOrCreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        GameObject textObject = GetOrCreateChild(name, parent, typeof(RectTransform), typeof(Text));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        ConfigureRect(rect, position, size);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = GetRuntimeFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private Font GetRuntimeFont()
    {
        if (runtimeFont != null)
            return runtimeFont;

        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return runtimeFont;
    }

    private Button GetOrCreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, Color color, SuccessMenuButtonTrigger.ButtonAction triggerAction)
    {
        GameObject buttonObject = GetOrCreateChild(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        ConfigureRect(rect, position, new Vector2(330f, 84f));

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);

        GetOrCreateText(buttonObject.transform, "Label", label, 27, Vector2.zero, new Vector2(310f, 70f), Color.white);
        ConfigurePhysicalButton(buttonObject, triggerAction);
        return button;
    }

    private void ConfigurePhysicalButton(GameObject buttonObject, SuccessMenuButtonTrigger.ButtonAction triggerAction)
    {
        SuccessMenuButtonTrigger trigger = buttonObject.GetComponent<SuccessMenuButtonTrigger>();
        if (trigger == null)
            trigger = buttonObject.AddComponent<SuccessMenuButtonTrigger>();

        trigger.controller = this;
        trigger.action = triggerAction;
        trigger.EnsureColliderSetup();
        trigger.ArmAfterDelay();
    }

    private void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.anchoredPosition3D = new Vector3(position.x, position.y, 0f);
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private void PlaceInFrontOfCamera()
    {
        Camera targetCamera = FindBestCamera();
        if (targetCamera == null) return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = targetCamera;
            canvas.sortingOrder = 520;
        }

        Transform cameraTransform = targetCamera.transform;
        transform.SetParent(null, true);
        transform.position = cameraTransform.position +
                             cameraTransform.forward * distanceFromCamera +
                             cameraTransform.TransformVector(cameraOffset);
        transform.rotation = cameraTransform.rotation;
        transform.localScale = Vector3.one * worldSpaceScale;

        Debug.Log("Success menu shown in front of camera: " + targetCamera.name);
    }

    private Camera FindBestCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
            return mainCamera;

        string[] preferredCameraNames = { "CenterEyeAnchor", "Main Camera", "LeftEyeAnchor", "RightEyeAnchor" };
        foreach (string cameraName in preferredCameraNames)
        {
            GameObject cameraObject = GameObject.Find(cameraName);
            if (cameraObject == null) continue;

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera != null && camera.isActiveAndEnabled)
                return camera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        if (EnsureOptionalComponent(eventSystem.gameObject, "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule"))
        {
            DisableNonXrInputModules(eventSystem);
            return;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private void EnsureTrackedDeviceRaycaster()
    {
        EnsureOptionalComponent(gameObject, "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster");
    }

    private void DisableNonXrInputModules(EventSystem eventSystem)
    {
        BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
        foreach (BaseInputModule module in modules)
        {
            if (module == null) continue;
            if (module.GetType().FullName == "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule")
                continue;

            module.enabled = false;
        }
    }

    private bool EnsureOptionalComponent(GameObject target, string typeName)
    {
        Type componentType = FindType(typeName);
        if (componentType == null)
            return false;

        try
        {
            if (target.GetComponent(componentType) == null)
                target.AddComponent(componentType);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Optional UI component not added: " + typeName + " | " + exception.Message);
            return false;
        }

        return true;
    }

    private Type FindType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }

    private void PlayFallbackSuccess()
    {
        if (audioSource == null)
            return;

        if (successClip == null)
            successClip = CreateSuccessClip();

        audioSource.Stop();
        audioSource.PlayOneShot(successClip, 0.75f);
    }

    private AudioClip CreateSuccessClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.55f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float frequency = time < 0.26f ? 660f : 880f;
            float envelope = Mathf.Clamp01(1f - time / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * 0.28f * envelope;
        }

        AudioClip clip = AudioClip.Create("GeneratedSuccessTone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
