using UnityEngine;

public class SuccessMenuButtonTrigger : MonoBehaviour
{
    public enum ButtonAction
    {
        Continue,
        Restart
    }

    public SuccessMenuController controller;
    public ButtonAction action;
    public string handLayerName = "PlayerHand";
    public float initialActivationDelay = 0.8f;
    public float activationCooldown = 0.6f;

    private float armedTime;
    private float nextActivationTime;

    private void Reset()
    {
        EnsureColliderSetup();
    }

    private void Awake()
    {
        EnsureColliderSetup();
    }

    private void OnEnable()
    {
        ArmAfterDelay();
    }

    public void ArmAfterDelay()
    {
        armedTime = Time.unscaledTime + initialActivationDelay;
        nextActivationTime = armedTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void TryActivate(Collider other)
    {
        if (Time.unscaledTime < armedTime) return;
        if (Time.unscaledTime < nextActivationTime) return;
        if (!HandContactUtility.IsHandOrController(other, handLayerName)) return;

        if (controller == null)
            controller = GetComponentInParent<SuccessMenuController>();

        if (controller == null) return;

        nextActivationTime = Time.unscaledTime + activationCooldown;

        if (action == ButtonAction.Continue)
        {
            Debug.Log("Success menu physical button: Continue");
            controller.ContinueToNextScene();
        }
        else
        {
            Debug.Log("Success menu physical button: Restart");
            controller.RestartModule();
        }
    }

    public void EnsureColliderSetup()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        boxCollider.isTrigger = true;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(360f, 120f, 120f);

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;
    }
}
