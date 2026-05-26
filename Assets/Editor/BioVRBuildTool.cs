using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

public static class BioVRBuildTool
{
    [MenuItem("BioVR/Build Android APK")]
    public static void BuildAndroidAPK()
    {
        string buildDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
        if (!Directory.Exists(buildDirectory))
        {
            Directory.CreateDirectory(buildDirectory);
        }

        string apkPath = Path.Combine(buildDirectory, "BioSandboxVR.apk");

        // 1. Get enabled scenes in build settings
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BioVR Build] No scenes enabled in Build Settings!");
            EditorUtility.DisplayDialog("Build Error", "No scenes are enabled in Build Settings. Please add your scenes in File > Build Settings.", "OK");
            return;
        }

        // 2. Switch build target to Android if it isn't already
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[BioVR Build] Switching active build target to Android...");
            bool switchSuccess = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            if (!switchSuccess)
            {
                Debug.LogError("[BioVR Build] Failed to switch build target to Android!");
                EditorUtility.DisplayDialog("Build Error", "Failed to switch build target to Android. Make sure you have Android Build Support installed in Unity Hub.", "OK");
                return;
            }
        }

        // 3. Perform the build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = apkPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log($"[BioVR Build] Starting Android Build at: {apkPath}");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BioVR Build] Build succeeded! Size: {summary.totalSize} bytes");
            EditorUtility.DisplayDialog("Build Success", $"Successfully built APK to:\n{apkPath}\n\nOpening the build folder...", "OK");
            EditorUtility.RevealInFinder(apkPath);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("[BioVR Build] Build failed!");
            EditorUtility.DisplayDialog("Build Failed", "Build failed. Please check the Console window for details.", "OK");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabledScenes = new System.Collections.Generic.List<string>();
        foreach (var scene in scenes)
        {
            if (scene.enabled)
            {
                enabledScenes.Add(scene.path);
            }
        }
        return enabledScenes.ToArray();
    }
}
