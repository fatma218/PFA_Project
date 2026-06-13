using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class XRSimulatorCleanup
{
    static XRSimulatorCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            var xrManager = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager;
            if (xrManager != null && xrManager.isInitializationComplete)
            {
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
                Debug.Log("[XRCleanup] XR ferme proprement.");
            }
        }
    }
}
#endif