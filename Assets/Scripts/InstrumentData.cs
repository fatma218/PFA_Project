using UnityEngine;

[CreateAssetMenu(fileName = "NewInstrument", menuName = "Inventory/Instrument")]
public class InstrumentData : ScriptableObject
{
    public string instrumentName;
    public Sprite icon; // L'image à afficher dans l'UI
    public GameObject prefab; // Le prefab à faire apparaître
}