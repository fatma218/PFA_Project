using UnityEngine;

/// <summary>
/// Teleportation pour le Simulator avec raycast visuel depuis la main.
///
/// CONTROLES :
///   Maintenir T  = affiche le rayon laser depuis la main vers le sol
///   Relacher T   = teleporte au point vise (si c'est la zone lavabo)
///   Y            = revenir au point de depart
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SimulatorTeleportHelper : MonoBehaviour
{
    [Header("References")]
    public Transform cameraRig;               // [BuildingBlock] Camera Rig
    public Transform handOrigin;              // point de depart du rayon (main droite)
    public Transform teleportZone;            // TeleportZone_Lavabo

    [Header("Positions")]
    public Vector3 sinkPosition  = new Vector3(-0.11f, 0f, 0.7f);
    public Vector3 startPosition = new Vector3(0.321f, 0f, -1.089f);

    [Header("Raycast visuel")]
    public float rayLength       = 4f;
    public Color rayColorAiming  = new Color(1f, 0.4f, 0.1f, 1f);   // orange quand on vise
    public Color rayColorValid   = new Color(0.2f, 1f, 0.3f, 1f);   // vert quand on vise la zone
    public float dotSize         = 0.04f;

    [Header("Touche")]
    public KeyCode aimKey        = KeyCode.T;
    public KeyCode returnKey     = KeyCode.Y;

    private LineRenderer laser;
    private GameObject hitDot;
    private bool isAiming = false;

    void Start()
    {
        // Laser
        laser = GetComponent<LineRenderer>();
        laser.positionCount = 2;
        laser.startWidth    = 0.008f;
        laser.endWidth      = 0.003f;
        laser.material      = new Material(Shader.Find("Sprites/Default"));
        laser.enabled       = false;
        laser.useWorldSpace = true;

        // Point de hit (sphere au bout du rayon)
        hitDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitDot.transform.localScale = Vector3.one * dotSize;
        hitDot.GetComponent<Collider>().enabled = false;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = rayColorAiming;
        hitDot.GetComponent<Renderer>().material = mat;
        hitDot.SetActive(false);

        // Si aucune main definie, utiliser la camera comme origine
        if (handOrigin == null && Camera.main != null)
            handOrigin = Camera.main.transform;
    }

    void Update()
    {
        // --- Retour au depart ---
        if (Input.GetKeyDown(returnKey))
        {
            DoTeleport(startPosition);
            Debug.Log("[SimHelper] Retour au depart (Y)");
        }

        // --- Mode visee : maintenir T ---
        if (Input.GetKey(aimKey))
        {
            isAiming = true;
            UpdateLaser();
        }

        // --- Relacher T = teleporter ---
        if (Input.GetKeyUp(aimKey) && isAiming)
        {
            isAiming = false;
            laser.enabled = false;
            hitDot.SetActive(false);

            // Verifier si on visait la zone lavabo
            if (IsAimingAtSinkZone())
            {
                DoTeleport(sinkPosition);
                Debug.Log("[SimHelper] Teleporte au lavabo !");
            }
            else
            {
                Debug.Log("[SimHelper] Pas dans la zone — relache sans teleport");
            }
        }
    }

    void UpdateLaser()
    {
        if (handOrigin == null) return;

        // Direction : vers le bas a 45 degres depuis la main
        Vector3 origin    = handOrigin.position;
        Vector3 direction = (Vector3.forward + Vector3.down * 1.2f).normalized;

        // Si on a un cameraRig, utiliser sa direction forward
        if (cameraRig != null)
            direction = (cameraRig.forward + Vector3.down * 1.2f).normalized;

        // Raycast sol
        bool hitGround = Physics.Raycast(origin, direction, out RaycastHit hit, rayLength);
        Vector3 endpoint = hitGround ? hit.point : origin + direction * rayLength;

        // Couleur selon si on vise la zone
        bool onZone = IsAimingAtSinkZone(endpoint);
        Color col   = onZone ? rayColorValid : rayColorAiming;

        // Dessiner le laser
        laser.enabled = true;
        laser.SetPosition(0, origin);
        laser.SetPosition(1, endpoint);
        laser.startColor = col;
        laser.endColor   = new Color(col.r, col.g, col.b, 0f);

        // Dessiner le point de hit
        hitDot.SetActive(true);
        hitDot.transform.position = endpoint;
        hitDot.GetComponent<Renderer>().material.color = col;
    }

    bool IsAimingAtSinkZone()
    {
        if (handOrigin == null || cameraRig == null) return false;
        Vector3 origin    = handOrigin.position;
        Vector3 direction = (cameraRig.forward + Vector3.down * 1.2f).normalized;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength))
            return IsAimingAtSinkZone(hit.point);
        return false;
    }

    bool IsAimingAtSinkZone(Vector3 point)
    {
        if (teleportZone == null) return false;
        float dist = Vector2.Distance(
            new Vector2(point.x, point.z),
            new Vector2(teleportZone.position.x, teleportZone.position.z)
        );
        return dist < 0.5f;
    }

    void DoTeleport(Vector3 pos)
    {
        if (cameraRig != null)
        {
            // Conserver la hauteur actuelle du rig
            pos.y = cameraRig.position.y;
            cameraRig.position = pos;
        }
    }
}
