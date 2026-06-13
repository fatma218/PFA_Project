using UnityEngine;

/// <summary>
/// Rotation continue d'un instrument 3D devant la IconCamera.
/// </summary>
public class InstrumentRotator : MonoBehaviour
{
    public float rotationSpeed = 0f; // degrees par seconde
    public Vector3 rotationAxis = Vector3.up;

    // void Update()
    // {
    //     transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.World);
    // }
}
