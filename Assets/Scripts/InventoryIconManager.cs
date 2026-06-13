using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryIconManager : MonoBehaviour
{
    [System.Serializable]
    public class InstrumentSlot
    {
        public string name;
        public GameObject prefab;
        public RawImage rawImage;
        public Vector3 rotation = new Vector3(0f, 0f, 0f);
        public float scale = 0.05f;
    }

    [Header("Slots — un par instrument")]
    public List<InstrumentSlot> slots = new List<InstrumentSlot>();

    [Header("Reglages communs")]
    public int renderTextureSize = 256;
    public float fieldOfView     = 30f;
    public float cameraDistance  = 0.3f;

    private const int BASE_LAYER = 20;
    private List<GameObject>    instances = new List<GameObject>();
    private List<Camera>        cameras   = new List<Camera>();
    private List<RenderTexture> textures  = new List<RenderTexture>();

    void Start()
    {
        foreach (var cam in FindObjectsOfType<Camera>())
            for (int i = 0; i < 10; i++)
                cam.cullingMask &= ~(1 << (BASE_LAYER + i));

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.prefab == null)
            {
                Debug.LogError("[Inventory] Slot " + i + " — prefab manquant !");
                continue;
            }
            if (slot.rawImage == null)
            {
                Debug.LogError("[Inventory] Slot " + i + " — rawImage manquant !");
                continue;
            }
            CreateSlot(i);
        }
    }

    void CreateSlot(int index)
    {
        var slot  = slots[index];
        int layer = BASE_LAYER + index;

        Vector3 displayPos = new Vector3(0f, -50f - (index * 3f), 0f);

        // RenderTexture
        var rt = new RenderTexture(renderTextureSize, renderTextureSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        rt.name = "RT_" + slot.name;
        rt.Create();
        textures.Add(rt);

        // Wrapper
        var wrapper = new GameObject("IconDisplay_" + slot.name);
        wrapper.transform.position   = displayPos;
        wrapper.transform.rotation   = Quaternion.Euler(slot.rotation);
        wrapper.transform.localScale = Vector3.one * slot.scale;

        // Prefab sous le wrapper — sans rotation
        var prefabGO = Instantiate(slot.prefab);
        prefabGO.transform.SetParent(wrapper.transform, false);
        prefabGO.transform.localPosition = Vector3.zero;
        prefabGO.transform.localRotation = Quaternion.identity;
        prefabGO.transform.localScale    = Vector3.one;
        SetLayerRecursive(wrapper, layer);
        instances.Add(wrapper);

        // Camera isolee
        var camGO = new GameObject("IconCam_" + slot.name);
        var cam   = camGO.AddComponent<Camera>();
        Vector3 camPos = displayPos + new Vector3(0f, 0f, -cameraDistance);
        camGO.transform.position = camPos;
        camGO.transform.LookAt(displayPos);

        cam.targetTexture   = rt;
        cam.cullingMask     = (1 << layer);
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.fieldOfView     = fieldOfView;
        cam.nearClipPlane   = 0.01f;
        cam.farClipPlane    = 20f;
        cam.depth           = -10 - index;
        cam.allowHDR        = false;
        cameras.Add(cam);

        // Assigner RT au RawImage
        slot.rawImage.texture = rt;
        slot.rawImage.color   = Color.white;

        Debug.Log("[Inventory] Slot " + index + " OK — " + slot.name);
    }

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    void OnDestroy()
    {
        foreach (var rt in textures)
            if (rt != null) { rt.Release(); Destroy(rt); }
        foreach (var go in instances)
            if (go != null) Destroy(go);
        foreach (var cam in cameras)
            if (cam != null) Destroy(cam.gameObject);
    }
}
