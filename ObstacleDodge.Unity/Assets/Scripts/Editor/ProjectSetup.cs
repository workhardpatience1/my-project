using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Runs automatically when the project opens:
//  - makes sure the "Hit" tag exists (guide 1, Part 4.4);
//  - adds LEVELPLAY_INSTALLED when the Unity LevelPlay package is installed (ads guide, Part 5.4),
//    so AdManager uses the real SDK on Android, and the project still builds without it;
//  - applies the recommended Player Settings once (package name, landscape, IL2CPP + ARM64...).
// Menu: Obstacle Dodge -> Apply recommended settings.
[InitializeOnLoad]
public static class ProjectSetup
{
    public const string PackageName = "com.workhardpatience.obstacledodge";
    const string LevelPlayDefine = "LEVELPLAY_INSTALLED";

    static ProjectSetup()
    {
        EditorApplication.delayCall += () =>
        {
            EnsureTag("Hit");
            UpdateLevelPlayDefine();
            ApplyIfDefault();
        };
    }

    [MenuItem("Obstacle Dodge/Apply recommended settings")]
    public static void ApplySettings()
    {
        PlayerSettings.companyName = "WorkHardPatience";
        PlayerSettings.productName = "Obstacle Dodge";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageName);

        // Android (guide 1 Part 9.3 and ads guide Part 5.5)
        PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.forceInternetPermission = true;

        // Landscape only (the track is wide)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // WebGL (ads guide Part 8.3)
        PlayerSettings.WebGL.template = "PROJECT:ObstacleDodge";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        EnsureTag("Hit");
        UpdateLevelPlayDefine();
        AssetDatabase.SaveAssets();
        Debug.Log("[Obstacle Dodge] Recommended settings applied (package " + PackageName + ")");
    }

    static void ApplyIfDefault()
    {
        string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (string.IsNullOrEmpty(id) || id.StartsWith("com.DefaultCompany") || id.StartsWith("com.UnityTechnologies") || id.StartsWith("com.Company"))
            ApplySettings();
    }

    public static void EnsureTag(string tag)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var tagManager = new SerializedObject(assets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log("[Obstacle Dodge] Tag added: " + tag);
    }

    public static void UpdateLevelPlayDefine()
    {
        bool installed = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetType("Unity.Services.LevelPlay.LevelPlay", false) != null);
        foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
            var list = new List<string>(defines);
            bool has = list.Contains(LevelPlayDefine);
            if (installed == has) continue;
            if (installed) list.Add(LevelPlayDefine); else list.Remove(LevelPlayDefine);
            PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
            Debug.Log("[Obstacle Dodge] " + LevelPlayDefine + (installed ? " added" : " removed") + " for " + target.TargetName);
        }
    }
}
