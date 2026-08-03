using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tikatooka.Editor
{
    public static class Ensure2DProjectSettings
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tikatooka/Use Stable Windows Build Graphics")]
        public static void UseStableWindowsGraphics()
        {
            var target = BuildTarget.StandaloneWindows64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });
            AssetDatabase.SaveAssets();
            Debug.Log("Tikatooka: Windows standalone builds are fixed to Direct3D 11.");
        }

        [MenuItem("Tikatooka/Optimize Game View")]
        public static void OptimizeGameView()
        {
            var gameViewType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.GameView", false))
                .FirstOrDefault(type => type != null);
            var vSyncField = gameViewType?.GetField(
                "m_VSyncEnabled",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var gameViews = gameViewType == null
                ? Array.Empty<UnityEngine.Object>()
                : Resources.FindObjectsOfTypeAll(gameViewType);

            if (vSyncField == null || gameViews.Length == 0)
            {
                Debug.LogWarning("Tikatooka: An open Game view was not found, so Game view VSync was not changed.");
                return;
            }

            foreach (var gameView in gameViews)
            {
                vSyncField.SetValue(gameView, true);
                EditorUtility.SetDirty(gameView);
                if (gameView is EditorWindow window)
                {
                    window.Repaint();
                }
            }

            Debug.Log($"Tikatooka: Enabled Game view VSync for {gameViews.Length} window(s).");
        }

        [MenuItem("Tikatooka/Repair Main Camera")]
        private static void RepairMainCamera()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != MainScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Tikatooka",
                    "Main 씬을 연 뒤 다시 실행해 주세요.",
                    "확인");
                return;
            }

            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(13, 39, 34, 255);

            cameraObject.AddComponent<AudioListener>();

            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }
}
