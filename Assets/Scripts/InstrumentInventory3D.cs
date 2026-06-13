using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class InstrumentInventory3D : MonoBehaviour
{
    [System.Serializable]
    public class InstrumentSlot
    {
        public GameObject prefab;
        public Vector3 localOffset;
        public Vector3 rotation;
        public float scale = 0.08f;
    }

    [Header("Canvas de reference")]
    public Transform canvasTransform;

    [Header("Slots instruments")]
    public InstrumentSlot[] slots;

    [Header("Reglages")]
    public float distanceFromCanvas = 0.08f;
    public float snapDistance = 0.15f;
    public float returnSpeed = 5f;

    void Start()
    {
        if (canvasTransform == null)
        {
            var c = GameObject.Find("Canvas");
            if (c != null) canvasTransform = c.transform;
        }

        for (int i = 0; i < slots.Length; i++)
            CreateSlot(slots[i], i);
    }

    void CreateSlot(InstrumentSlot slot, int index)
    {
        if (slot.prefab == null) return;

        Vector3 slotPos = canvasTransform.position
                        + canvasTransform.forward * distanceFromCanvas
                        + canvasTransform.TransformDirection(slot.localOffset);

        // Socket visuel
        GameObject socketGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        socketGO.name = "Socket_" + slot.prefab.name;
        socketGO.transform.position   = slotPos;
        socketGO.transform.rotation   = canvasTransform.rotation;
        socketGO.transform.localScale = new Vector3(0.1f, 0.002f, 0.1f);
        Destroy(socketGO.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        socketGO.GetComponent<Renderer>().material = mat;

        // Instrument
        GameObject go = Instantiate(slot.prefab, slotPos, Quaternion.Euler(slot.rotation));
        go.name = "Instrument_" + slot.prefab.name;
        go.transform.localScale = Vector3.one * slot.scale;

        // Rigidbody
        Rigidbody rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.mass = 0.1f;
        rb.drag = 5f;
        rb.angularDrag = 5f;
        rb.useGravity = false;
        rb.isKinematic = true;

        // Collider
        if (go.GetComponentInChildren<Collider>() == null)
        {
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.06f, 0.02f, 0.18f);
        }

        // Grabbable
        Grabbable grabbable = go.GetComponent<Grabbable>() ?? go.AddComponent<Grabbable>();

        // GrabInteractable
        GrabInteractable grabInteractable = go.GetComponent<GrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = go.AddComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rb);
        }

        // HandGrabInteractable
        HandGrabInteractable handGrab = go.GetComponent<HandGrabInteractable>();
        if (handGrab == null)
        {
            handGrab = go.AddComponent<HandGrabInteractable>();
            handGrab.InjectRigidbody(rb);
        }

        // Returner
        var returner = go.AddComponent<InstrumentSlotReturner>();
        returner.slotPosition = slotPos;
        returner.slotRotation = Quaternion.Euler(slot.rotation);
        returner.snapDistance = snapDistance;
        returner.returnSpeed  = returnSpeed;
        returner.socketVisual = socketGO;
        returner.grabbable    = grabbable;
        returner.rb           = rb;

        Debug.Log("[Inventory3D] Slot " + index + " — " + go.name + " a " + slotPos);
    }
}
