using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// One-click builds (menu "Obstacle Dodge") and the entry points for command line / CI:
//   Unity -batchmode -quit -projectPath . -executeMethod BuildScript.BuildAndroid
public static class BuildScript
{
    static readonly string[] Scenes = { "Assets/Scenes/SampleScene.unity" };

    [MenuItem("Obstacle Dodge/Build Android APK")]
    public static void BuildAndroid()
    {
        ProjectSetup.ApplySettings();
        EditorUserBuildSettings.buildAppBundle = false;
        Build(BuildTarget.Android, "Builds/Android/ObstacleDodge.apk");
    }

    [MenuItem("Obstacle Dodge/Build Android App Bundle (Google Play)")]
    public static void BuildAndroidBundle()
    {
        ProjectSetup.ApplySettings();
        EditorUserBuildSettings.buildAppBundle = true;
        Build(BuildTarget.Android, "Builds/Android/ObstacleDodge.aab");
    }

    [MenuItem("Obstacle Dodge/Build WebGL")]
    public static void BuildWebGL()
    {
        ProjectSetup.ApplySettings();
        Build(BuildTarget.WebGL, "Builds/WebGL");
    }

    [MenuItem("Obstacle Dodge/Build Windows")]
    public static void BuildWindows()
    {
        ProjectSetup.ApplySettings();
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/ObstacleDodge.exe");
    }

    static void Build(BuildTarget target, string path)
    {
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[Obstacle Dodge] Build " + summary.result + ": " + path + " (" + summary.totalSize / (1024 * 1024) + " MB)");
        if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
