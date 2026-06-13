using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FailureMenuController : MonoBehaviour
{
    [Header("Text")]
    public string title = "CONTAMINATION";
    public string defaultReason = "Vous avez touche une surface non sterile.";
    public string restartLabel = "Recommencer";
    public string quitLabel = "Exit";

    [Header("Placement")]
    public float distanceFromCamera = 0.45f;
    public Vector3 cameraOffset = new Vector3(-0.12f, 0.1f, 0f);
    public float worldSpaceScale = 0.0008f;
    public bool useScreenSpaceOverlayInEditor = false;

    [Header("Scene red tint")]
    public bool tintWholeView = true;
    public Color sceneTintColor = new Color(0.85f, 0f, 0f, 0.32f);
    public float sceneTintDistance = 0.58f;
    public Vector2 sceneTintSize = new Vector2(2200f, 1400f);
    public float sceneTintScale = 0.001f;

    [Header("Navigation")]
    public string quitSceneName = "MainMenu";
    public bool restartCurrentSceneIfNoWashingManager = true;

    [Header("Feedback")]
    public Color redOverlayColor = new Color(0.75f, 0f, 0f, 0.52f);
    public Color panelColor = new Color(0.22f, 0.02f, 0.02f, 0.96f);
    public bool playFallbackAlertSound = true;

    private Text reasonText;
    private AudioSource alertAudioSource;
    private AudioClip fallbackAlertClip;
    private Font runtimeFont;
    private GameObject sceneTintObject;
    private bool uiBuilt;

    private void Awake()
    {
        EnsureUI();
    }

    private void OnEnable()
    {
        EnsureUI();

        if (Application.isPlaying)
        {
            if (ShouldUseScreenSpaceOverlay())
                PlaceAsScreenOverlay();
            else
                PlaceInFrontOfCamera();
        }
    }

    public void Show(string reason)
    {
        enabled = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureUI();
        RefreshUIOrder();

        if (ShouldUseScreenSpaceOverlay())
            PlaceAsScreenOverlay();
        else
            PlaceInFrontOfCamera();

        if (reasonText != null)
            reasonText.text = string.IsNullOrEmpty(reason) ? defaultReason : reason;

        if (playFallbackAlertSound)
            PlayFallbackAlert();
    }

    private void OnDisable()
    {
        HideSceneTint();
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

    public void QuitModule()
    {
        if (!string.IsNullOrEmpty(quitSceneName))
        {
            SceneManager.LoadScene(quitSceneName);
            return;
        }

        Application.Quit();
    }

    private void EnsureUI()
    {
        if (uiBuilt)
            return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;

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
            root.sizeDelta = new Vector2(1100f, 650f);
            root.localScale = Vector3.one * 0.0016f;
        }

        GameObject overlay = GetOrCreateChild("RedSceneOverlay", transform, typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        ConfigureRect(overlayRect, Vector2.zero, new Vector2(1150f, 700f));
        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = redOverlayColor;
        overlayImage.raycastTarget = false;
        overlay.transform.SetAsFirstSibling();

        GameObject panel = GetOrCreateChild("FailureMenuPanel", transform, typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        ConfigureRect(panelRect, Vector2.zero, new Vector2(860f, 480f));
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = panelColor;
        panel.transform.SetAsLastSibling();

        Text warningText = GetOrCreateText(panel.transform, "WarningSign", "!", 96, new Vector2(0f, 160f), new Vector2(120f, 110f), new Color(1f, 0.88f, 0.08f, 1f));
        warningText.fontStyle = FontStyle.Bold;

        Text titleText = GetOrCreateText(panel.transform, "Title", title, 54, new Vector2(0f, 75f), new Vector2(760f, 75f), Color.white);
        titleText.fontStyle = FontStyle.Bold;

        reasonText = GetOrCreateText(panel.transform, "Reason", defaultReason, 32, new Vector2(0f, -25f), new Vector2(760f, 115f), Color.white);

        GetOrCreateButton(panel.transform, "RestartButton", restartLabel, new Vector2(-180f, -165f), RestartModule, new Color(0.05f, 0.32f, 0.95f, 1f), FailureMenuButtonTrigger.ButtonAction.Restart);
        GetOrCreateButton(panel.transform, "QuitButton", quitLabel, new Vector2(180f, -165f), QuitModule, new Color(0.35f, 0.035f, 0.035f, 1f), FailureMenuButtonTrigger.ButtonAction.Quit);

        alertAudioSource = GetComponent<AudioSource>();
        if (alertAudioSource == null)
            alertAudioSource = gameObject.AddComponent<AudioSource>();

        uiBuilt = true;
    }

    private void RefreshUIOrder()
    {
        Transform overlay = transform.Find("RedSceneOverlay");
        if (overlay != null)
        {
            Image overlayImage = overlay.GetComponent<Image>();
            if (overlayImage != null)
                overlayImage.raycastTarget = false;

            overlay.SetAsFirstSibling();
        }

        Transform panel = transform.Find("FailureMenuPanel");
        if (panel != null)
            panel.SetAsLastSibling();
    }

    private GameObject GetOrCreateChild(string childName, Transform parent, params System.Type[] components)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);

            foreach (System.Type componentType in components)
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
        if (runtimeFont == null)
            Debug.LogWarning("LegacyRuntime.ttf font not found. Failure menu text may use Unity fallback rendering.");

        return runtimeFont;
    }

    private Button GetOrCreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, Color color, FailureMenuButtonTrigger.ButtonAction triggerAction)
    {
        GameObject buttonObject = GetOrCreateChild(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        ConfigureRect(rect, position, new Vector2(270f, 84f));

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);

        GetOrCreateText(buttonObject.transform, "Label", label, 30, Vector2.zero, new Vector2(250f, 70f), Color.white);
        ConfigurePhysicalButton(buttonObject, triggerAction);
        return button;
    }

    private void ConfigurePhysicalButton(GameObject buttonObject, FailureMenuButtonTrigger.ButtonAction triggerAction)
    {
        FailureMenuButtonTrigger trigger = buttonObject.GetComponent<FailureMenuButtonTrigger>();
        if (trigger == null)
            trigger = buttonObject.AddComponent<FailureMenuButtonTrigger>();

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

        if (targetCamera == null)
        {
            Canvas fallbackCanvas = GetComponent<Canvas>();
            if (fallbackCanvas != null)
                fallbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Debug.LogWarning("Fail menu shown without camera: using Screen Space Overlay fallback.");
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = targetCamera;
            canvas.sortingOrder = 500;
        }

        Transform cameraTransform = targetCamera.transform;
        transform.SetParent(null, true);
        transform.position = cameraTransform.position +
                             cameraTransform.forward * distanceFromCamera +
                             cameraTransform.TransformVector(cameraOffset);
        transform.rotation = cameraTransform.rotation;
        transform.localScale = Vector3.one * worldSpaceScale;

        Debug.Log("Fail menu shown in front of camera: " + targetCamera.name +
                  " | distance=" + distanceFromCamera +
                  " | offset=" + cameraOffset +
                  " | scale=" + worldSpaceScale);

        ShowSceneTint(targetCamera);
    }

    private bool ShouldUseScreenSpaceOverlay()
    {
#if UNITY_EDITOR
        return useScreenSpaceOverlayInEditor;
#else
        return false;
#endif
    }

    private void PlaceAsScreenOverlay()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.sortingOrder = 500;
        }

        transform.SetParent(null, false);

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.anchoredPosition3D = Vector3.zero;
            root.sizeDelta = new Vector2(1100f, 650f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
        }

        Debug.Log("Fail menu shown as Screen Space Overlay for Editor/Simulator.");
    }

    private Camera FindBestCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
            return mainCamera;

        string[] preferredCameraNames =
        {
            "CenterEyeAnchor",
            "Main Camera",
            "LeftEyeAnchor",
            "RightEyeAnchor"
        };

        foreach (string cameraName in preferredCameraNames)
        {
            GameObject cameraObject = GameObject.Find(cameraName);
            if (cameraObject == null) continue;

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera != null && camera.isActiveAndEnabled)
                return camera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.isActiveAndEnabled)
                return camera;
        }

        return cameras.Length > 0 ? cameras[0] : null;
    }

    private void ShowSceneTint(Camera targetCamera)
    {
        if (!tintWholeView || targetCamera == null)
        {
            HideSceneTint();
            return;
        }

        if (sceneTintObject == null)
        {
            sceneTintObject = new GameObject("FailureSceneRedTint", typeof(RectTransform), typeof(Canvas));
            GameObject tintImageObject = new GameObject("Tint", typeof(RectTransform), typeof(Image));
            tintImageObject.transform.SetParent(sceneTintObject.transform, false);

            RectTransform imageRect = tintImageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Image image = tintImageObject.GetComponent<Image>();
            image.raycastTarget = false;
        }

        sceneTintObject.SetActive(true);
        sceneTintObject.transform.SetParent(targetCamera.transform, false);
        sceneTintObject.transform.localPosition = new Vector3(0f, 0f, sceneTintDistance);
        sceneTintObject.transform.localRotation = Quaternion.identity;
        sceneTintObject.transform.localScale = Vector3.one * sceneTintScale;

        RectTransform root = sceneTintObject.transform as RectTransform;
        if (root != null)
        {
            root.sizeDelta = sceneTintSize;
            root.localRotation = Quaternion.identity;
        }

        Canvas canvas = sceneTintObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;
        canvas.sortingOrder = 490;

        Image tintImage = sceneTintObject.GetComponentInChildren<Image>(true);
        if (tintImage != null)
        {
            tintImage.color = sceneTintColor;
            tintImage.raycastTarget = false;
        }
    }

    private void HideSceneTint()
    {
        if (sceneTintObject != null)
            sceneTintObject.SetActive(false);
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

    private void PlayFallbackAlert()
    {
        if (alertAudioSource == null)
            return;

        if (fallbackAlertClip == null)
            fallbackAlertClip = CreateAlertClip();

        alertAudioSource.Stop();
        alertAudioSource.PlayOneShot(fallbackAlertClip, 0.85f);
    }

    private AudioClip CreateAlertClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.65f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float pulse = Mathf.Repeat(time * 5f, 1f) < 0.55f ? 1f : 0f;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * time) * 0.35f * pulse;
        }

        AudioClip clip = AudioClip.Create("GeneratedContaminationAlert", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
