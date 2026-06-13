using UnityEngine;

public static class HandContactUtility
{
    private const int MaxParentDepth = 12;

    public static bool IsHandOrController(Collider other, string handLayerName = "PlayerHand")
    {
        if (other == null) return false;

        int handLayer = LayerMask.NameToLayer(handLayerName);
        Transform current = other.transform;
        int depth = 0;

        while (current != null && depth++ < MaxParentDepth)
        {
            GameObject go = current.gameObject;
            string objectName = go.name.ToLowerInvariant();
            string layerName = LayerMask.LayerToName(go.layer).ToLowerInvariant();

            if (handLayer != -1 && go.layer == handLayer)
                return true;

            if (layerName == "playerhand" ||
                layerName == "hands" ||
                layerName == "controller" ||
                layerName == "handleft" ||
                layerName == "handright")
                return true;

            if (LooksLikeHandOrControllerName(objectName))
                return true;

            if (HasHandTrackingComponent(go))
                return true;

            current = current.parent;
        }

        return false;
    }

    public static string GetHandId(Collider other)
    {
        if (other == null) return "UnknownHand";

        string path = GetHierarchyPath(other.transform).ToLowerInvariant();

        if (path.Contains("left") || path.Contains("_l_") || path.Contains("lefth"))
            return "LeftHand";

        if (path.Contains("right") || path.Contains("_r_") || path.Contains("righth"))
            return "RightHand";

        Transform root = FindContactRoot(other.transform);
        return root != null ? root.GetInstanceID().ToString() : other.GetInstanceID().ToString();
    }

    public static void EnsureKinematicRigidbody(GameObject target)
    {
        if (target == null) return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
            rb = target.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public static void AssignLayerRecursively(GameObject target, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (target == null || layer == -1) return;

        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    private static bool LooksLikeHandOrControllerName(string objectName)
    {
        return objectName.Contains("hand") ||
               objectName.Contains("controller") ||
               objectName.Contains("anchor") ||
               objectName.Contains("pointer") ||
               objectName.Contains("index") ||
               objectName.Contains("thumb") ||
               objectName.Contains("middle") ||
               objectName.Contains("ring") ||
               objectName.Contains("pinky") ||
               objectName.Contains("wrist") ||
               objectName.Contains("palm") ||
               objectName.Contains("finger") ||
               objectName.Contains("handbone") ||
               objectName.Contains("b_l_") ||
               objectName.Contains("b_r_");
    }

    private static bool HasHandTrackingComponent(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null) continue;

            string typeName = component.GetType().Name.ToLowerInvariant();
            if (typeName.Contains("ovrhand") ||
                typeName.Contains("ovrskeleton") ||
                typeName.Contains("handvisual") ||
                typeName.Contains("handtracking"))
                return true;
        }

        return false;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;
        int depth = 0;

        while (current != null && depth++ < MaxParentDepth)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static Transform FindContactRoot(Transform transform)
    {
        Transform current = transform;
        Transform best = transform;
        int depth = 0;

        while (current != null && depth++ < MaxParentDepth)
        {
            string name = current.name.ToLowerInvariant();
            if (LooksLikeHandOrControllerName(name) || HasHandTrackingComponent(current.gameObject))
                best = current;

            current = current.parent;
        }

        return best;
    }
}
