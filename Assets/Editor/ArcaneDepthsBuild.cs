#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ArcaneDepthsBuild
{
    private const string ScenePath = "Assets/Scenes/DungeonPrototype.unity";
    private const string WindowsBuildPath = "Builds/Windows/ArcaneDepths.exe";

    [MenuItem("Arcane Depths/Configure Player Settings")]
    public static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Arcane Depths Studio";
        PlayerSettings.productName = "Arcane Depths";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Branding/ArcaneDepthsAppIcon.png");
        if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new[] { icon });
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Arcane Depths/Build Windows x64")]
    public static void BuildWindows64()
    {
        ConfigurePlayerSettings();
        Directory.CreateDirectory(Path.GetDirectoryName(WindowsBuildPath) ?? "Builds");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = WindowsBuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.StrictMode
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Windows build failed: " + report.summary.result);
    }
}
#endif
