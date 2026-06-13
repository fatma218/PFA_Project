using UnityEngine;
using UnityEditor;

/// <summary>
/// Outil Editor pour assigner automatiquement tous les AudioClips du projet VR médical.
/// Menu : Tools > Audio Setup Tool
/// </summary>
public class AudioSetupTool : EditorWindow
{
    // ──────────────────────────────────────────────────────────────────────────
    // Chemins des fichiers audio (relatifs à Assets/)
    // ──────────────────────────────────────────────────────────────────────────

    // Sons connus avec certitude
    private const string PATH_WATER       = "Assets/Audio/265330__evil-dog__bathroom-sink-water-running.wav";
    private const string PATH_SOAP_PUMP   = "Assets/Audio/656267__miaopolus__soap-pump-single-pump.wav";
    private const string PATH_SUCCESS     = "Assets/Audio/607926__robinhood76__10661-bonus-correct-answer.wav";
    private const string PATH_ALERT       = "Assets/Audio/581604__samsterbirdies__beep-error-2.wav";

    // Instructions vocales nommées (identifiées par leur nom de fichier)
    private const string PATH_VOICE_SCRUB  = "Assets/Audio/Frottez toutes les surfaces de vos mains pendant 30 secondes..mp3";
    private const string PATH_VOICE_RINSE  = "Assets/Audio/Rincez soigneusement vos mains.9.mp3";
    private const string PATH_VOICE_DRY    = "Assets/Audio/Séchez vos mains avec une serviette stérile..mp3";
    private const string PATH_VOICE_CONTAM = "Assets/Audio/Contanimationdetecté.mp3";

    // Instructions vocales inconnues (ttsMP3 sans nom clair)
    // → Assignées dans l'ordre logique du workflow : WetHands → TakeSoap → (extra)
    private const string PATH_VOICE_WET_HANDS  = "Assets/Audio/ttsMP3.com_VoiceText_2026-5-22_10-18-4.mp3";
    private const string PATH_VOICE_TAKE_SOAP  = "Assets/Audio/ttsMP3.com_VoiceText_2026-5-22_10-21-29.mp3";
    private const string PATH_VOICE_EXTRA      = "Assets/Audio/ttsMP3.com_VoiceText_2026-5-22_10-22-2.mp3";
    // Note : PATH_VOICE_EXTRA est assigné à voiceContamination comme fallback.
    // Si tu veux tester ces 3 fichiers, utilise le bouton "▶ Tester" ci-dessous.

    // ──────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Audio Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<AudioSetupTool>("Audio Setup Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🎧 Audio Setup — VR Formation Médicale", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Ce bouton assigne automatiquement tous les AudioClips identifiés\n" +
            "dans HandWashingManager, SoapController, WaterController et DryingController.\n\n" +
            "⚠️ Les 3 fichiers ttsMP3 sans nom clair sont assignés dans l'ordre logique.\n" +
            "Utilise les boutons 'Tester' pour vérifier et corriger si besoin.",
            MessageType.Info
        );

        EditorGUILayout.Space(8);

        if (GUILayout.Button("✅  Assigner TOUS les sons automatiquement", GUILayout.Height(40)))
        {
            AssignAllSounds();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🔊 Tester les fichiers inconnus", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Clique sur ▶ pour écouter chaque fichier ttsMP3 et vérifier ce qu'il contient.\n" +
            "Si l'assignation est incorrecte, dis-le et on corrigera.",
            MessageType.None
        );

        EditorGUILayout.Space(4);

        DrawTestButton("10-18-4 (assigné → voiceWetHands)", PATH_VOICE_WET_HANDS);
        DrawTestButton("10-21-29 (assigné → voiceTakeSoap)", PATH_VOICE_TAKE_SOAP);
        DrawTestButton("10-22-2 (assigné → extra/backup)", PATH_VOICE_EXTRA);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("📋 Mapping complet", EditorStyles.boldLabel);
        DrawMapping("voiceWetHands",    PATH_VOICE_WET_HANDS);
        DrawMapping("voiceTakeSoap",    PATH_VOICE_TAKE_SOAP);
        DrawMapping("voiceScrub",       PATH_VOICE_SCRUB);
        DrawMapping("voiceRinse",       PATH_VOICE_RINSE);
        DrawMapping("voiceDry",         PATH_VOICE_DRY);
        DrawMapping("voiceContamination", PATH_VOICE_CONTAM);
        DrawMapping("soundSuccess",     PATH_SUCCESS);
        DrawMapping("soundAlert",       PATH_ALERT);
        DrawMapping("soapPumpSound",    PATH_SOAP_PUMP);
        DrawMapping("waterSound",       PATH_WATER);
    }

    private void DrawTestButton(string label, string path)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(280));
        if (GUILayout.Button("▶ Tester", GUILayout.Width(80)))
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                PlayClipInEditor(clip);
            else
                Debug.LogWarning($"[AudioSetupTool] Fichier introuvable : {path}");
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMapping(string slot, string path)
    {
        string filename = System.IO.Path.GetFileName(path);
        bool exists = System.IO.File.Exists(Application.dataPath + "/../" + path);
        string status = exists ? "✅" : "❌";
        EditorGUILayout.LabelField($"  {status}  {slot}  ←  {filename}", EditorStyles.miniLabel);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logique d'assignation
    // ──────────────────────────────────────────────────────────────────────────

    private void AssignAllSounds()
    {
        int count = 0;

        // ── HandWashingManager ──────────────────────────────────────────────
        HandWashingManager hwm = FindObjectOfType<HandWashingManager>();
        if (hwm != null)
        {
            SerializedObject so = new SerializedObject(hwm);

            SetClip(so, "voiceWetHands",    PATH_VOICE_WET_HANDS,  ref count);
            SetClip(so, "voiceTakeSoap",    PATH_VOICE_TAKE_SOAP,  ref count);
            SetClip(so, "voiceScrub",       PATH_VOICE_SCRUB,      ref count);
            SetClip(so, "voiceRinse",       PATH_VOICE_RINSE,      ref count);
            SetClip(so, "voiceDry",         PATH_VOICE_DRY,        ref count);
            SetClip(so, "voiceContamination", PATH_VOICE_CONTAM,   ref count);
            SetClip(so, "soundSuccess",     PATH_SUCCESS,          ref count);
            SetClip(so, "soundAlert",       PATH_ALERT,            ref count);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hwm);
            Debug.Log("[AudioSetupTool] ✅ HandWashingManager mis à jour.");
        }
        else
        {
            Debug.LogWarning("[AudioSetupTool] ⚠️ HandWashingManager introuvable dans la scène.");
        }

        // ── SoapController ──────────────────────────────────────────────────
        SoapController soap = FindObjectOfType<SoapController>();
        if (soap != null)
        {
            AudioClip soapClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SOAP_PUMP);
            if (soapClip != null)
            {
                SerializedObject so = new SerializedObject(soap);
                // soapPumpSound est un AudioSource, on assigne le clip sur l'AudioSource existant
                AudioSource soapSource = soap.soapPumpSound;
                if (soapSource != null)
                {
                    SerializedObject soSource = new SerializedObject(soapSource);
                    soSource.FindProperty("m_audioClip").objectReferenceValue = soapClip;
                    soSource.ApplyModifiedProperties();
                    EditorUtility.SetDirty(soapSource);
                    count++;
                    Debug.Log("[AudioSetupTool] ✅ SoapController.soapPumpSound mis à jour.");
                }
                else
                {
                    Debug.LogWarning("[AudioSetupTool] ⚠️ SoapController.soapPumpSound (AudioSource) est null — assigne-le d'abord dans l'Inspector.");
                }
            }
        }
        else
        {
            Debug.LogWarning("[AudioSetupTool] ⚠️ SoapController introuvable dans la scène.");
        }

        // ── WaterController ─────────────────────────────────────────────────
        WaterController water = FindObjectOfType<WaterController>();
        if (water != null)
        {
            AudioClip waterClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_WATER);
            if (waterClip != null && water.waterSound != null)
            {
                SerializedObject soSource = new SerializedObject(water.waterSound);
                soSource.FindProperty("m_audioClip").objectReferenceValue = waterClip;
                soSource.ApplyModifiedProperties();
                EditorUtility.SetDirty(water.waterSound);
                count++;
                Debug.Log("[AudioSetupTool] ✅ WaterController.waterSound mis à jour.");
            }
            else
            {
                Debug.LogWarning("[AudioSetupTool] ⚠️ WaterController.waterSound (AudioSource) est null — assigne-le d'abord dans l'Inspector.");
            }
        }
        else
        {
            Debug.LogWarning("[AudioSetupTool] ⚠️ WaterController introuvable dans la scène.");
        }

        // ── DryingController ────────────────────────────────────────────────
        DryingController dry = FindObjectOfType<DryingController>();
        if (dry != null)
        {
            // soundTrashDrop : on utilise le son d'alerte comme son de poubelle (adapté)
            // Remplace PATH_ALERT par un son de poubelle dédié si tu en as un
            AudioClip trashClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_ALERT);
            if (trashClip != null)
            {
                SerializedObject so = new SerializedObject(dry);
                var prop = so.FindProperty("soundTrashDrop");
                if (prop != null)
                {
                    prop.objectReferenceValue = trashClip;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(dry);
                    count++;
                    Debug.Log("[AudioSetupTool] ✅ DryingController.soundTrashDrop mis à jour.");
                }
            }
        }
        else
        {
            Debug.LogWarning("[AudioSetupTool] ⚠️ DryingController introuvable dans la scène.");
        }

        AssetDatabase.SaveAssets();

        if (count > 0)
            EditorUtility.DisplayDialog("Audio Setup", $"✅ {count} clips assignés avec succès !", "OK");
        else
            EditorUtility.DisplayDialog("Audio Setup", "⚠️ Aucun clip assigné. Vérifie que la scène PreparationRoom est ouverte.", "OK");
    }

    private void SetClip(SerializedObject so, string fieldName, string assetPath, ref int count)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioSetupTool] ❌ Fichier introuvable : {assetPath}");
            return;
        }

        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[AudioSetupTool] ❌ Propriété '{fieldName}' introuvable.");
            return;
        }

        prop.objectReferenceValue = clip;
        count++;
        Debug.Log($"[AudioSetupTool] ✅ {fieldName} ← {System.IO.Path.GetFileName(assetPath)}");
    }

    // Joue un clip directement dans l'éditeur (sans entrer en Play Mode)
    private static void PlayClipInEditor(AudioClip clip)
    {
        var assembly = typeof(AudioImporter).Assembly;
        var utilityType = assembly.GetType("UnityEditor.AudioUtil");
        if (utilityType == null)
        {
            Debug.Log($"[AudioSetupTool] Impossible de jouer '{clip.name}' dans l'éditeur — utilise Play Mode.");
            return;
        }
        var method = utilityType.GetMethod(
            "PlayPreviewClip",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );
        if (method != null)
            method.Invoke(null, new object[] { clip, 0, false });
        else
            Debug.Log($"[AudioSetupTool] Clip prêt : {clip.name} ({clip.length:F1}s) — joue-le depuis le Project window.");
    }
}
