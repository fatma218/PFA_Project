using System.Collections;
using UnityEngine;

public class HandColliderAdder : MonoBehaviour
{
    private OVRSkeleton skeleton;

    private IEnumerator Start()
    {
        float timeout = 5f;
        while (skeleton == null && timeout > 0f)
        {
            skeleton = FindMatchingSkeleton();
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        HandContactUtility.AssignLayerRecursively(gameObject, "PlayerHand");
        HandContactUtility.EnsureKinematicRigidbody(gameObject);

        if (skeleton == null)
        {
            Debug.LogWarning("OVRSkeleton not found for " + name + ". Visual hand collider only.");
            yield break;
        }

        yield return new WaitUntil(() => skeleton.IsInitialized &&
                                         skeleton.Bones != null &&
                                         skeleton.Bones.Count > 0);

        Debug.Log("Skeleton initialized with " + skeleton.Bones.Count + " bones.");

        foreach (OVRBone bone in skeleton.Bones)
        {
            GameObject go = bone.Transform.gameObject;

            SphereCollider col = go.GetComponent<SphereCollider>();
            if (col == null)
                col = go.AddComponent<SphereCollider>();

            col.radius = 0.025f;
            col.isTrigger = false;

            HandContactUtility.EnsureKinematicRigidbody(go);

            int handLayer = LayerMask.NameToLayer("PlayerHand");
            if (handLayer != -1)
                go.layer = handLayer;

            Debug.Log("Collider added: " + go.name);
        }
    }

    private OVRSkeleton FindMatchingSkeleton()
    {
        OVRSkeleton found = GetComponent<OVRSkeleton>();
        if (found != null) return found;

        found = GetComponentInChildren<OVRSkeleton>(true);
        if (found != null) return found;

        found = GetComponentInParent<OVRSkeleton>();
        if (found != null) return found;

        string path = GetHierarchyPath(transform).ToLowerInvariant();
        bool wantsLeft = path.Contains("left");
        bool wantsRight = path.Contains("right");

        foreach (OVRSkeleton candidate in FindObjectsOfType<OVRSkeleton>(true))
        {
            string candidatePath = GetHierarchyPath(candidate.transform).ToLowerInvariant();
            if (wantsLeft && candidatePath.Contains("left"))
                return candidate;

            if (wantsRight && candidatePath.Contains("right"))
                return candidate;
        }

        return null;
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
}
