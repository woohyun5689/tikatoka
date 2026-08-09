using System.Linq;
using Tikatooka;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tikatooka.Editor
{
    public static class ProjectSetup
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tikatooka/Create Main Scene")]
        public static void CreateMainScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null
                && !EditorUtility.DisplayDialog(
                    "Main 씬 다시 만들기",
                    "기존 Main 씬을 새 씬으로 덮어씁니다. 계속할까요?",
                    "덮어쓰기",
                    "취소"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Create2DCamera();

            var controllerObject = new GameObject("Dice Dominion Game");
            controllerObject.AddComponent<DiceBoardGameController>();

            EnsureBootCover.CreateOrUpdate(scene);

            EditorSceneManager.SaveScene(scene, MainScenePath);
            var otherScenes = EditorBuildSettings.scenes
                .Where(sceneEntry => sceneEntry.path != MainScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) }
                .Concat(otherScenes)
                .ToArray();

            PlayerSettings.companyName = "Tikatooka";
            PlayerSettings.productName = "Dice Dominion";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;

            AssetDatabase.SaveAssets();
        }

        private static void Create2DCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(13, 39, 34, 255);

            cameraObject.AddComponent<AudioListener>();
        }
    }
}
