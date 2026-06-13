using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TeleportZoneVisual : MonoBehaviour
{
    [Header("Cercle — taille")]
    public float radius = 0.18f;       // petit cercle au sol
    public int segments = 48;

    [Header("Animation — pulse lent")]
    public float pulseSpeed = 0.8f;    // lent
    public float widthMin = 0.008f;
    public float widthMax = 0.022f;

    [Header("Couleur")]
    public Color ringColor = new Color(1f, 0.15f, 0.1f, 1f);   // rouge

    [Header("Anneau de propagation")]
    public bool showPropagation = true;
    public float propagationSpeed = 0.4f;       // lent
    public float propagationMaxRadius = 0.30f;  // petit

    private LineRenderer ring;
    private LineRenderer propRing;
    private float timer;
    private float propR;

    void Start()
    {
        // Anneau principal
        ring = GetComponent<LineRenderer>();
        ring.loop = true;
        ring.positionCount = segments + 1;
        ring.useWorldSpace = false;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.startWidth = widthMin;
        ring.endWidth   = widthMin;
        ring.startColor = ringColor;
        ring.endColor   = ringColor;
        DrawCircle(ring, radius);

        // Anneau propagation
        if (showPropagation)
        {
            GameObject go = new GameObject("PropRing");
            go.transform.SetParent(transform, false);
            propRing = go.AddComponent<LineRenderer>();
            propRing.loop = true;
            propRing.positionCount = segments + 1;
            propRing.useWorldSpace = false;
            propRing.material = new Material(Shader.Find("Sprites/Default"));
            propRing.startWidth = 0.012f;
            propRing.endWidth   = 0.004f;
            propR = radius;
        }
    }

    void Update()
    {
        timer += Time.deltaTime * pulseSpeed;

        // Pulse largeur
        float t = (Mathf.Sin(timer) + 1f) / 2f;
        float w = Mathf.Lerp(widthMin, widthMax, t);
        ring.startWidth = w;
        ring.endWidth   = w;

        // Pulse alpha
        float alpha = Mathf.Lerp(0.45f, 1f, t);
        Color c = ringColor; c.a = alpha;
        ring.startColor = c;
        ring.endColor   = c;

        // Propagation
        if (showPropagation && propRing != null)
        {
            propR += propagationSpeed * Time.deltaTime;
            if (propR > propagationMaxRadius) propR = radius;

            float fade = 1f - (propR - radius) / (propagationMaxRadius - radius);
            Color pc = new Color(1f, 0.2f, 0.1f, fade * 0.7f);
            propRing.startColor = pc;
            propRing.endColor   = new Color(1f, 0.2f, 0.1f, 0f);
            DrawCircle(propRing, propR);
        }
    }

    void DrawCircle(LineRenderer lr, float r)
    {
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
        }
    }
}
