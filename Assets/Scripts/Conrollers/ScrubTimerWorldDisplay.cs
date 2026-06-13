using UnityEngine;

public class ScrubTimerWorldDisplay : MonoBehaviour
{
    public GameObject displayRoot;
    public TextMesh timerText;
    public string readyText = "00:00";

    private Renderer[] displayRenderers;
    private Collider[] displayColliders;
    private int lastDisplayedSeconds = -1;

    private void Start()
    {
        CacheVisuals();
        SetVisible(false);
        UpdateText(readyText);
    }

    private void Update()
    {
        HandWashingManager manager = HandWashingManager.Instance;
        bool isScrubbing = manager != null &&
                           manager.CurrentStep == HandWashingManager.WashStep.ScrubbingHands;

        SetVisible(isScrubbing);

        if (!isScrubbing) return;

        int remaining = Mathf.CeilToInt(manager.ScrubbingRemainingTime);
        if (remaining == lastDisplayedSeconds) return;

        lastDisplayedSeconds = remaining;
        int minutes = remaining / 60;
        int seconds = remaining % 60;
        UpdateText(string.Format("{0:00}:{1:00}", minutes, seconds));
    }

    private void CacheVisuals()
    {
        if (displayRoot == null)
            displayRoot = gameObject;

        if (timerText == null)
            timerText = GetComponentInChildren<TextMesh>(true);

        displayRenderers = displayRoot.GetComponentsInChildren<Renderer>(true);
        displayColliders = displayRoot.GetComponentsInChildren<Collider>(true);
    }

    private void SetVisible(bool visible)
    {
        if (displayRoot != null && displayRoot != gameObject && displayRoot.activeSelf != visible)
        {
            displayRoot.SetActive(visible);
            return;
        }

        if (displayRenderers != null)
        {
            foreach (Renderer displayRenderer in displayRenderers)
            {
                if (displayRenderer != null)
                    displayRenderer.enabled = visible;
            }
        }

        if (displayColliders != null)
        {
            foreach (Collider displayCollider in displayColliders)
            {
                if (displayCollider != null)
                    displayCollider.enabled = visible;
            }
        }
    }

    private void UpdateText(string value)
    {
        if (timerText != null)
            timerText.text = value;
    }
}
