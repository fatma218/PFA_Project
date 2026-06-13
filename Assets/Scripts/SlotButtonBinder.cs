using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Branche automatiquement le Button.OnClick vers InstrumentSpawner.SpawnInstrument()
/// au demarrage — evite de faire le drag-drop manuel dans l'Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(InstrumentSpawner))]
public class SlotButtonBinder : MonoBehaviour
{
    void Awake()
    {
        var button  = GetComponent<Button>();
        var spawner = GetComponent<InstrumentSpawner>();

        if (button == null || spawner == null) return;

        // Nettoyer les anciens listeners et brancher SpawnInstrument
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(spawner.SpawnInstrument);

        Debug.Log("[SlotBinder] Button branche sur SpawnInstrument — " + gameObject.name);
    }
}
