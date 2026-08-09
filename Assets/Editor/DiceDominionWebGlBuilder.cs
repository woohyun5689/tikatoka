using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tikatooka.Editor
{
    [InitializeOnLoad]
    public static class DiceDominionWebGlBuilder
    {
        private const string RequestFileName = "DiceDominion-WebGL.request";
        private const string OutputFolderName = "DiceDominion-WebGL";
        private static bool buildQueued;

        static DiceDominionWebGlBuilder()
        {
            EditorApplication.update -= CheckForRequestedBuild;
            EditorApplication.update += CheckForRequestedBuild;
        }

        [MenuItem("Dice Dominion/Build WebGL")]
        public static void BuildWebGl()
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning("Dice Dominion: a player build is already in progress.");
                return;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Dice Dominion: no enabled scenes are configured for the WebGL build.");
                return;
            }

            var outputPath = GetOutputPath();
            Directory.CreateDirectory(outputPath);

            // Keep the output usable on simple static hosts that do not set gzip response headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            AssetDatabase.SaveAssets();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Dice Dominion: WebGL build failed. See the Console for details. Output: {outputPath}");
                return;
            }

            Debug.Log($"Dice Dominion: WebGL build completed successfully. Output: {outputPath}");
        }

        private static void CheckForRequestedBuild()
        {
            if (buildQueued
                || EditorApplication.isPlayingOrWillChangePlaymode
                || BuildPipeline.isBuildingPlayer
                || !File.Exists(GetRequestPath()))
            {
                return;
            }

            File.Delete(GetRequestPath());
            buildQueued = true;
            EditorApplication.delayCall += () =>
            {
                buildQueued = false;
                BuildWebGl();
            };
        }

        private static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetRequestPath()
        {
            return Path.Combine(GetProjectPath(), "Library", RequestFileName);
        }

        private static string GetOutputPath()
        {
            return Path.Combine(Path.GetPathRoot(GetProjectPath()) ?? GetProjectPath(), OutputFolderName);
        }
    }
}
