using UnityEngine;

public class TrashZone : MonoBehaviour
{
    [Header("Référence au DryingController")]
    public DryingController dryingController;

    [Tooltip("Nom (ou partie du nom) du tissu pour le reconnaître")]
    public string tissueName = "Paper Towel";

    void OnTriggerEnter(Collider other)
    {
        if (dryingController == null) return;

        string objName = other.gameObject.name.ToLower();
        if (objName.Contains(tissueName.ToLower()))
        {
            Debug.Log("🗑️ Tissu détecté dans la poubelle");
            dryingController.OnTissueThrownInTrash(other.gameObject);
        }
    }
}