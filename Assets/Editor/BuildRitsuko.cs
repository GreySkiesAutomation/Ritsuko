using System.IO;
using UnityEditor;

public static class BuildRitsuko
{
    public static void BuildMac()
    {
        var outputPath = "/Users/ritsuko/RitsukoBuild/Ritsuko.app";

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = outputPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new System.Exception("Mac build failed.");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabledScenePaths = new System.Collections.Generic.List<string>();

        for (var i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled)
            {
                enabledScenePaths.Add(scenes[i].path);
            }
        }

        return enabledScenePaths.ToArray();
    }
}