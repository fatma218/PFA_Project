using System.Collections;
using UnityEngine;

public class HandColliderSetup : MonoBehaviour
{
    [Header("Layer assigned to generated hand colliders")]
    public string handLayerName = "PlayerHand";

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

        EnsureVisualHandCollider();

        if (skeleton == null)
        {
            Debug.LogWarning("OVRSkeleton not found for " + name + ". Visual hand collider only.");
            yield break;
        }

        yield return new WaitUntil(() => skeleton.IsInitialized &&
                                         skeleton.Bones != null &&
                                         skeleton.Bones.Count > 0);

        Debug.Log("OVRSkeleton initialized. Adding hand colliders for " + name + ".");
        AddCollidersToHand();
    }

    private void AddCollidersToHand()
    {
        int handLayer = LayerMask.NameToLayer(handLayerName);

        OVRSkeleton.BoneId[] targetBones =
        {
            OVRSkeleton.BoneId.Hand_IndexTip,
            OVRSkeleton.BoneId.Hand_MiddleTip,
            OVRSkeleton.BoneId.Hand_ThumbTip,
            OVRSkeleton.BoneId.Hand_RingTip,
            OVRSkeleton.BoneId.Hand_PinkyTip,
            OVRSkeleton.BoneId.Hand_WristRoot,
        };

        foreach (OVRBone bone in skeleton.Bones)
        {
            foreach (OVRSkeleton.BoneId targetId in targetBones)
            {
                if (bone.Id != targetId) continue;

                GameObject boneObject = bone.Transform.gameObject;

                SphereCollider col = boneObject.GetComponent<SphereCollider>();
                if (col == null)
                    col = boneObject.AddComponent<SphereCollider>();

                col.radius = 0.025f;
                col.isTrigger = false;

                HandContactUtility.EnsureKinematicRigidbody(boneObject);

                if (handLayer != -1)
                    boneObject.layer = handLayer;

                Debug.Log("Hand collider added on " + bone.Transform.name + ".");
            }
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

    private void EnsureVisualHandCollider()
    {
        HandContactUtility.AssignLayerRecursively(gameObject, handLayerName);
        HandContactUtility.EnsureKinematicRigidbody(gameObject);
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
